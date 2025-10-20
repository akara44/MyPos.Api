using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using System;
using MyPos.Domain.Entities;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


// NOT: Eğer TransactionType enum'u buraya dahil edilemezse, aşağıdaki kodda MyPos.Domain.Entities.TransactionType şeklinde tam yol kullanılmıştır.

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SummaryController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public SummaryController(MyPosDbContext context)
    {
        _context = context;
    }

    // =================================================================
    // GET: api/summary/daily-totals
    // ... (GetDailyTotals metodu önceki halinde kaldı)
    // =================================================================

    /// <summary>
    /// Sadece bugüne ait ödeme tipi bazlı detaylı SATIŞLAR ve borç özetini listeler.
    /// (Satış raporu)
    /// </summary>
    [HttpGet("daily-totals")]
    public async Task<ActionResult<DailySummaryDto>> GetDailyTotals()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece bugünün başlangıcını ve bitişini hesapla
        var queryDate = DateTime.Today;
        var startOfDay = queryDate.Date;
        var endOfDay = startOfDay.AddDays(1);

        // 1. O Güne Ait Tamamlanmış Satışları Çek
        var completedSales = await _context.Sales
            .Where(s => s.UserId == currentUserId &&
                        s.IsCompleted && // Sadece tamamlanmış satışlar
                        s.SaleDate >= startOfDay &&
                        s.SaleDate < endOfDay)
            .ToListAsync();

        // 2. Ödeme Tiplerine Göre Detaylı Toplamları Hesapla
        decimal singleCashTotal = 0;
        decimal singlePosTotal = 0;
        decimal singleOpenAccountTotal = 0;

        decimal splitCashTotal = 0;
        decimal splitPosTotal = 0;

        // Nakit, POS, Açık Hesap dışındaki tüm ödemeler (tekli ve parçalı) nakit kasasına yönlendirilir.
        decimal totalToCashBucket = 0;

        foreach (var sale in completedSales)
        {
            if (sale.PaymentType.Equals("Nakit", StringComparison.OrdinalIgnoreCase))
            {
                singleCashTotal += sale.TotalAmount;
            }
            else if (sale.PaymentType.Equals("POS", StringComparison.OrdinalIgnoreCase))
            {
                singlePosTotal += sale.TotalAmount;
            }
            else if (sale.PaymentType.Equals("Açık Hesap", StringComparison.OrdinalIgnoreCase))
            {
                singleOpenAccountTotal += sale.TotalAmount;
            }
            else if (sale.PaymentType.Equals("Parçalı", StringComparison.OrdinalIgnoreCase))
            {
                var splitPayments = await _context.Payments
                    .Where(p => p.SaleId == sale.SaleId)
                    .ToListAsync();

                foreach (var payment in splitPayments)
                {
                    if (payment.Note.Equals("Nakit", StringComparison.OrdinalIgnoreCase))
                    {
                        splitCashTotal += payment.Amount;
                    }
                    else if (payment.Note.Equals("POS", StringComparison.OrdinalIgnoreCase))
                    {
                        splitPosTotal += payment.Amount;
                    }
                    else if (payment.Note.Equals("Açık Hesap", StringComparison.OrdinalIgnoreCase))
                    {
                        // Parçalı Açık Hesap ödemesi toplama dahil edilmiyor.
                    }
                    else
                    {
                        // Nakit, POS, Açık Hesap dışındaki tüm parçalı ödemeleri Nakit Toplamına yönlendir.
                        totalToCashBucket += payment.Amount;
                    }
                }
            }
            else
            {
                // Diğer tekli ödeme tiplerini (EFT, Yemek Çeki vb.) Nakit Toplamına yönlendir.
                totalToCashBucket += sale.TotalAmount;
            }
        }

        // 3. Doğrudan Borç Ekleme İşlemlerini Çek
        var directDebtsTotal = await _context.Debts
            .Where(d => d.UserId == currentUserId &&
                        d.DebtDate >= startOfDay &&
                        d.DebtDate < endOfDay)
            .SumAsync(d => (decimal?)d.Amount) ?? 0;

        // 4. Nihai Toplamları Hesapla
        var totalCash = singleCashTotal + splitCashTotal + totalToCashBucket;
        var totalPos = singlePosTotal + splitPosTotal;
        var totalOpenAccount = singleOpenAccountTotal;

        decimal grandTotalSales = totalCash + totalPos + totalOpenAccount;

        var summaryDto = new DailySummaryDto
        {
            SingleCashSales = singleCashTotal,
            SplitCashSales = splitCashTotal + totalToCashBucket,
            SinglePosSales = singlePosTotal,
            SplitPosSales = splitPosTotal,
            SingleOpenAccountSales = singleOpenAccountTotal,

            TotalCash = totalCash,
            TotalPos = totalPos,
            TotalOpenAccount = totalOpenAccount,
            TotalSales = grandTotalSales,

            DirectDebtAdditions = directDebtsTotal
        };

        return Ok(summaryDto);
    }


    // =================================================================
    // GET: api/summary/daily-cash-flow
    // =================================================================
    /// <summary>
    /// Daily Reports: Bugüne ait Müşteri Tahsilatları, Firma Ödemeleri, Gelir ve Giderlerin Nakit/POS bazlı özetini listeler.
    /// </summary>
    [HttpGet("daily-cash-flow")]
    public async Task<ActionResult<DailyCashFlowReportDto>> GetDailyCashFlowReport()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece bugünün başlangıcını ve bitişini hesapla
        var queryDate = DateTime.Today;
        var startOfDay = queryDate.Date;
        var endOfDay = startOfDay.AddDays(1);

        var report = new DailyCashFlowReportDto();

        // --------------------------------------------------------------
        // 1. Müşteri Tahsilatları (PaymentsController) - GİRİŞ
        // --------------------------------------------------------------
        var customerPayments = await _context.Payments
            .Where(p => p.UserId == currentUserId &&
                        p.PaymentDate >= startOfDay &&
                        p.PaymentDate < endOfDay &&
                        p.SaleId == null) // Sadece borç tahsilatlarını al (satış ödemelerini hariç tut)
            .ToListAsync();

        report.CustomerPayments.Total = customerPayments.Sum(p => p.Amount);
        report.CustomerPayments.Cash = customerPayments
            .Where(p => p.PaymentType != null && p.PaymentType.Name.Equals("Nakit", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Amount);
        report.CustomerPayments.Pos = customerPayments
            .Where(p => p.PaymentType != null && p.PaymentType.Name.Equals("POS", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Amount);


        // --------------------------------------------------------------
        // 2. Firma Ödemeleri (CompanyTransactionsController) - ÇIKIŞ
        // --------------------------------------------------------------
        var companyPayments = await _context.CompanyTransactions
            .Include(t => t.Company)
            .Include(t => t.PaymentType) // PaymentType adını almak için eklendi
            .Where(t => t.Company.UserId == currentUserId &&
                        t.Type == MyPos.Domain.Entities.TransactionType.Payment && // Düzeltme: Type enum ve Payment (Ödeme)
                        t.TransactionDate >= startOfDay &&                       // Düzeltme: TransactionDate
                        t.TransactionDate < endOfDay)
            .ToListAsync();

        report.CompanyPayments.Total = companyPayments.Sum(t => t.Amount);
        report.CompanyPayments.Cash = companyPayments
            .Where(t => t.PaymentType != null && t.PaymentType.Name.Equals("NAKİT", StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.Amount);
        report.CompanyPayments.Pos = companyPayments
            .Where(t => t.PaymentType != null && (t.PaymentType.Name.Equals("POS", StringComparison.OrdinalIgnoreCase) ||
                        t.PaymentType.Name.Equals("KREDİ KARTI", StringComparison.OrdinalIgnoreCase)))
            .Sum(t => t.Amount);


        // --------------------------------------------------------------
        // 3. Diğer Gelirler (IncomeController) - GİRİŞ
        // --------------------------------------------------------------
        var incomes = await _context.Incomes
            .Where(i => i.UserId == currentUserId &&
                        i.Date >= startOfDay &&
                        i.Date < endOfDay)
            .ToListAsync();

        report.Incomes.Total = incomes.Sum(i => i.Amount);
        report.Incomes.Cash = incomes
            .Where(i => i.PaymentType != null && i.PaymentType.Equals("NAKİT", StringComparison.OrdinalIgnoreCase))
            .Sum(i => i.Amount);
        report.Incomes.Pos = incomes
            .Where(i => i.PaymentType != null && (i.PaymentType.Equals("POS", StringComparison.OrdinalIgnoreCase) ||
                        i.PaymentType.Equals("KREDİ KARTI", StringComparison.OrdinalIgnoreCase)))
            .Sum(i => i.Amount);


        // --------------------------------------------------------------
        // 4. Giderler (ExpenseController) - ÇIKIŞ
        // --------------------------------------------------------------
        var expenses = await _context.Expenses
            .Where(e => e.UserId == currentUserId &&
                        e.Date >= startOfDay &&
                        e.Date < endOfDay)
            .ToListAsync();

        report.Expenses.Total = expenses.Sum(e => e.Amount);
        report.Expenses.Cash = expenses
            .Where(e => e.PaymentType != null && e.PaymentType.Equals("NAKİT", StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.Amount);
        report.Expenses.Pos = expenses
            .Where(e => e.PaymentType != null && (e.PaymentType.Equals("KREDİ KARTI", StringComparison.OrdinalIgnoreCase) ||
                        e.PaymentType.Equals("POS", StringComparison.OrdinalIgnoreCase)))
            .Sum(e => e.Amount);


        // --------------------------------------------------------------
        // 5. GENEL NAKİT AKIŞI ÖZETİ (Girişler - Çıkışlar)
        // --------------------------------------------------------------

        // Satış özetini al
        var dailySalesSummaryResult = await GetDailyTotals();

        if (dailySalesSummaryResult.Result is OkObjectResult okResult && okResult.Value is DailySummaryDto salesDto)
        {
            // Girişler (Inflow)
            report.TotalCashInflow = salesDto.TotalCash + report.CustomerPayments.Cash + report.Incomes.Cash;
            report.TotalPosInflow = salesDto.TotalPos + report.CustomerPayments.Pos + report.Incomes.Pos;

            // Çıkışlar (Outflow)
            report.TotalCashOutflow = report.CompanyPayments.Cash + report.Expenses.Cash;
            report.TotalPosOutflow = report.CompanyPayments.Pos + report.Expenses.Pos;

            // Net Değişim
            report.NetCashChange = report.TotalCashInflow - report.TotalCashOutflow;
            report.NetPosChange = report.TotalPosInflow - report.TotalPosOutflow;
        }

        return Ok(report);
    }
    [HttpGet("comprehensive-daily-summary")]
    public async Task<ActionResult<ComprehensiveDailySummaryDto>> GetComprehensiveDailySummary()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var queryDate = DateTime.Today;
        var startOfDay = queryDate.ToUniversalTime();
        var endOfDay = queryDate.AddDays(1).ToUniversalTime();

        // 1. Tüm İlgili Verilerin Çekilmesi

        // Satışlar (Bugün) - SADECE SaleItems ile yüklenir. Payment koleksiyonuna ihtiyaç YOKTUR.
        var sales = await _context.Sales
            .Where(s => s.UserId == currentUserId && s.SaleDate >= startOfDay && s.SaleDate < endOfDay)
            .Include(s => s.SaleItems) // SADECE SaleItems mevcuttur
                .ThenInclude(si => si.Product)
            .ToListAsync();

        // ... (Diğer verilerin çekilmesi öncekiyle aynı) ...
        var incomes = await _context.Incomes
            .Where(i => i.UserId == currentUserId && i.Date >= startOfDay && i.Date < endOfDay)
            .ToListAsync();

        var expenses = await _context.Expenses
            .Where(e => e.UserId == currentUserId && e.Date >= startOfDay && e.Date < endOfDay)
            .ToListAsync();

        // Müşteri Ödemeleri (PaymentsController.cs'ten gelen yapıya uygun olarak)
        var customerPayments = await _context.Payments
            .Where(p => p.UserId == currentUserId && p.PaymentDate >= startOfDay && p.PaymentDate < endOfDay)
            .Include(p => p.PaymentType)
            .ToListAsync();

        var companyTransactions = await _context.CompanyTransactions
            .Where(ct => ct.Company.UserId == currentUserId &&
                         ct.Type == TransactionType.Payment &&
                         ct.TransactionDate >= startOfDay && ct.TransactionDate < endOfDay)
            .ToListAsync();

        var cashPurchaseInvoices = await _context.PurchaseInvoices
            .Where(pi => pi.UserId == currentUserId && pi.InvoiceDate >= startOfDay && pi.InvoiceDate < endOfDay)
            .Include(pi => pi.PaymentType)
            .ToListAsync();

        // =================================================================
        // 2. Hesaplamalar
        // =================================================================

        // Satış Hesaplamaları (Mevcut Sale modeline uyarlandı)
        // Burada parçalı/tekil ayrımı yapılamaz, tüm satışlar tekil ödeme kabul edilir.
        var totalSales = sales.Sum(s => s.TotalAmount);

        // Nakit Satışlar (İstenen raporlara göre tüm nakit girişleri)
        // NOT: Eğer modelde "NAKİT" PaymentType'ı varsa, tüm TotalAmount o kasaya girer.
        var cashSales = sales
            .Where(s => s.PaymentType == "NAKİT")
            .Sum(s => s.TotalAmount);

        // POS Satışlar
        var posSales = sales
            .Where(s => s.PaymentType == "POS" || s.PaymentType == "KREDİ KARTI")
            .Sum(s => s.TotalAmount);

        // Nakit Müşteri Ödemeleri (Tahsilat)
        var cashCustomerPayments = customerPayments
            .Where(p => p.PaymentType != null && p.PaymentType.Name == "NAKİT")
            .Sum(p => p.Amount);

        // POS/Kredi Kartı Müşteri Ödemeleri (Tahsilat)
        var posCustomerPayments = customerPayments
            .Where(p => p.PaymentType != null && p.PaymentType.Name != "NAKİT")
            .Sum(p => p.Amount);

        // Nakit Gelirler
        var cashIncomes = incomes
            .Where(i => i.PaymentType == "NAKİT")
            .Sum(i => i.Amount);

        // Nakit Firma Ödemeleri (-)
        var cashCompanyPayments = companyTransactions
            .Where(ct => ct.PaymentType != null && ct.PaymentType.Name.Equals("NAKİT", StringComparison.OrdinalIgnoreCase))
            .Sum(ct => ct.Amount);

        // Nakit Giderler (-)
        var cashExpenses = expenses
            .Where(e => e.PaymentType == "NAKİT")
            .Sum(e => e.Amount);

        // Nakit Alış Faturası Ödemeleri (-)
        var cashPurchaseInvoiceTotal = cashPurchaseInvoices
            .Where(pi => pi.PaymentType != null && pi.PaymentType.Name == "NAKİT")
            .Sum(pi => pi.GrandTotal);

        // Kar Hesaplaması
        decimal grossProfit = 0;
        decimal totalCostOfGoodsSold = 0;

        foreach (var sale in sales)
        {
            foreach (var item in sale.SaleItems)
            {
                // TotalPrice'ı alıp Discount'u uygula
                var totalItemSalesPrice = item.TotalPrice - item.Discount;
                var costPrice = item.Product.PurchasePrice;
                var totalItemCost = costPrice * item.Quantity;

                grossProfit += (totalItemSalesPrice - totalItemCost);
                totalCostOfGoodsSold += totalItemCost;
            }
        }

        // =================================================================
        // 3. DTO'ların Doldurulması
        // =================================================================

        var result = new ComprehensiveDailySummaryDto();

        // 1. Nakit Kasa Raporu
        // NOT: Tüm nakit satışlar tek bir başlıkta toplanmıştır.
        result.CashRegisterReport.CashSales = cashSales;
        result.CashRegisterReport.CashIncomes = cashIncomes;
        result.CashRegisterReport.CashCustomerPayments = cashCustomerPayments;
        result.CashRegisterReport.CashCompanyPayments = cashCompanyPayments;
        result.CashRegisterReport.CashExpenses = cashExpenses;
        result.CashRegisterReport.CashPurchaseInvoicePayments = cashPurchaseInvoiceTotal;

        result.CashRegisterReport.NetCashBalance =
            result.CashRegisterReport.CashSales +
            result.CashRegisterReport.CashIncomes +
            result.CashRegisterReport.CashCustomerPayments -
            result.CashRegisterReport.CashCompanyPayments -
            result.CashRegisterReport.CashExpenses -
            result.CashRegisterReport.CashPurchaseInvoicePayments;

        // 2. Kar Raporu
        result.ProfitReport.GrossProfit = grossProfit;

        // 3. Ciro Raporu
        // NOT: RevenueReportDto yerine RevenueSummaryDto kullanıldı.
        var totalIncomes = incomes.Sum(i => i.Amount);
        var totalExpenses = expenses.Sum(e => e.Amount);

        result.RevenueSummary.TotalSalesRevenue = totalSales; // Toplam Satış Tutarı
        result.RevenueSummary.CustomerPayments = cashCustomerPayments + posCustomerPayments; // Toplam Tahsilat
        result.RevenueSummary.Incomes = totalIncomes;
        result.RevenueSummary.Expenses = totalExpenses;

        result.RevenueSummary.NetRevenue =
            result.RevenueSummary.TotalSalesRevenue +
            result.RevenueSummary.CustomerPayments +
            result.RevenueSummary.Incomes -
            result.RevenueSummary.Expenses;

        // 4. Ürün Maliyeti Raporu
        result.ProductCostReport.TotalCostOfGoodsSold = totalCostOfGoodsSold;

        return Ok(result);
    }
}


// =================================================================
// DTO Yapıları
// =================================================================

// Kasa Tipine Göre Toplamları Tutan Alt DTO

// =================================================================
// DTO Yapıları (DailyFinancialSummaryDto ve Alt DTO'lar)
// =================================================================

// Kutu formatına uygun genel DTO
public class SummaryBoxDto
{
    public decimal Total { get; set; }
    public decimal Cash { get; set; }
    public decimal Pos { get; set; }
}

/// <summary>
/// Görseldeki 4 Kutu Özeti için Ana DTO
/// </summary>
public class CashFlowDetailDto
{
    public decimal Total { get; set; }
    public decimal Cash { get; set; }
    public decimal Pos { get; set; }
}

/// <summary>
/// Günlük Nakit Akışı Raporu için Ana DTO
/// </summary>
public class DailyCashFlowReportDto
{
    // Girişler (Tahsilatlar)
    public CashFlowDetailDto CustomerPayments { get; set; } = new CashFlowDetailDto(); // Müşterinin bize yaptığı ödemeler (Tahsilatlar)
    public CashFlowDetailDto Incomes { get; set; } = new CashFlowDetailDto();          // Diğer gelirler

    // Çıkışlar (Ödemeler)
    public CashFlowDetailDto CompanyPayments { get; set; } = new CashFlowDetailDto();   // Firma ödemeleri
    public CashFlowDetailDto Expenses { get; set; } = new CashFlowDetailDto();          // İşletme giderleri

    // Genel Akış Özeti (Satışlar da dahil edilerek hesaplanacak)
    public decimal TotalCashInflow { get; set; }
    public decimal TotalPosInflow { get; set; }

    public decimal TotalCashOutflow { get; set; }
    public decimal TotalPosOutflow { get; set; }

    public decimal NetCashChange { get; set; }
    public decimal NetPosChange { get; set; }
}

// (DailySummaryDto eski haliyle burada kalır)
public class DailySummaryDto
{
    public decimal SingleCashSales { get; set; }
    public decimal SplitCashSales { get; set; }
    public decimal SinglePosSales { get; set; }
    public decimal SplitPosSales { get; set; }
    public decimal SingleOpenAccountSales { get; set; }

    public decimal TotalCash { get; set; }
    public decimal TotalPos { get; set; }
    public decimal TotalOpenAccount { get; set; }

    public decimal TotalSales { get; set; }
    public decimal DirectDebtAdditions { get; set; }
}

// KONTROL: Buradaki RevenueReportDto'yu RevenueSummaryDto olarak değiştiriyoruz.
public class ComprehensiveDailySummaryDto
{
    public DailyCashRegisterReportDto CashRegisterReport { get; set; } = new DailyCashRegisterReportDto();
    public ProfitReportDto ProfitReport { get; set; } = new ProfitReportDto();
    public RevenueSummaryDto RevenueSummary { get; set; } = new RevenueSummaryDto(); // İSİM GÜNCELLENDİ
    public ProductCostReportDto ProductCostReport { get; set; } = new ProductCostReportDto();
}

public class DailyCashRegisterReportDto
{
    public decimal CashSales { get; set; }
    public decimal CashIncomes { get; set; }
    public decimal CashCustomerPayments { get; set; }
    public decimal CashCompanyPayments { get; set; }
    public decimal CashExpenses { get; set; }
    public decimal CashPurchaseInvoicePayments { get; set; }
    public decimal NetCashBalance { get; set; }
}

public class ProfitReportDto
{
    public decimal GrossProfit { get; set; }
}

// YENİ İSİM: RevenueSummaryDto
public class RevenueSummaryDto
{
    public decimal TotalSalesRevenue { get; set; }
    public decimal CustomerPayments { get; set; }
    public decimal Incomes { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetRevenue { get; set; }
}

public class ProductCostReportDto
{
    public decimal TotalCostOfGoodsSold { get; set; }
} 