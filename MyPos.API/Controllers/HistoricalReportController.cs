using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using System;
using MyPos.Domain.Entities;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MyPos.Application.Dtos.Reports;
using System.Collections.Generic;

// NOT: Eğer TransactionType enum'u buraya dahil edilemezse, aşağıdaki kodda MyPos.Domain.Entities.TransactionType şeklinde tam yol kullanılmıştır.

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class HistoricalReportController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public HistoricalReportController(MyPosDbContext context)
    {
        _context = context;
    }

    // =================================================================
    // GET: api/historicalreport/sales-totals
    // =================================================================

    /// <summary>
    /// Belirtilen tarih aralığına ait ödeme tipi bazlı detaylı SATIŞLAR ve borç özetini listeler.
    /// </summary>
    /// <param name="startDate">Rapor başlangıç tarihi (Örn: 2025-10-20)</param>
    /// <param name="startTime">Rapor başlangıç saati (Örn: 00:00:00)</param>
    /// <param name="endDate">Rapor bitiş tarihi (Örn: 2025-10-20)</param>
    /// <param name="endTime">Rapor bitiş saati (Örn: 23:59:59)</param>
    [HttpGet("sales-totals")]
    public async Task<ActionResult<DailySummaryDto>> GetHistoricalSalesTotals(
        [FromQuery] DateTime startDate,
        [FromQuery] TimeSpan startTime, // Güncellendi
        [FromQuery] DateTime endDate,
        [FromQuery] TimeSpan endTime) // Güncellendi
    {
        // Tarih ve saatleri birleştirerek tam DateTime nesnelerini oluştur
        var startFilter = startDate.Date.Add(startTime);
        var endFilter = endDate.Date.Add(endTime);

        if (startFilter >= endFilter)
        {
            return BadRequest("Başlangıç tarih ve saati, bitiş tarih ve saatinden küçük olmalıdır.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. O Tarih Aralığına Ait Tamamlanmış Satışları Çek
        var completedSales = await _context.Sales
            .Where(s => s.UserId == currentUserId &&
                        s.IsCompleted && // Sadece tamamlanmış satışlar
                        s.SaleDate >= startFilter &&
                        s.SaleDate < endFilter)
            .ToListAsync();

        // 2. Ödeme Tiplerine Göre Detaylı Toplamları Hesapla
        decimal singleCashTotal = 0;
        decimal singlePosTotal = 0;
        decimal singleOpenAccountTotal = 0;

        decimal splitCashTotal = 0;
        decimal splitPosTotal = 0;

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
                        totalToCashBucket += payment.Amount;
                    }
                }
            }
            else
            {
                totalToCashBucket += sale.TotalAmount;
            }
        }

        // 3. Doğrudan Borç Ekleme İşlemlerini Çek
        var directDebtsTotal = await _context.Debts
            .Where(d => d.UserId == currentUserId &&
                        d.DebtDate >= startFilter &&
                        d.DebtDate < endFilter)
            .SumAsync(d => (decimal?)d.Amount) ?? 0;

        // 4. Nihai Toplamları Hesapla
        var totalCash = singleCashTotal + splitCashTotal + totalToCashBucket;
        var totalPos = singlePosTotal + splitPosTotal;
        var totalOpenAccount = singleOpenAccountTotal;

        decimal grandTotalSales = totalCash + totalPos + totalOpenAccount;

        var summaryDto = new DailySummaryDto // DailySummaryDto mevcut haliyle kullanıldı
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
    // GET: api/historicalreport/cash-flow
    // =================================================================

    /// <summary>
    /// Tarihsel Raporlar: Belirtilen tarih aralığına ait Müşteri Tahsilatları, Firma Ödemeleri, Gelir ve Giderlerin Nakit/POS bazlı özetini listeler.
    /// </summary>
    /// <param name="startDate">Rapor başlangıç tarihi</param>
    /// <param name="startTime">Rapor başlangıç saati</param>
    /// <param name="endDate">Rapor bitiş tarihi</param>
    /// <param name="endTime">Rapor bitiş saati</param>
    [HttpGet("cash-flow")]
    public async Task<ActionResult<DailyCashFlowReportDto>> GetHistoricalCashFlowReport(
        [FromQuery] DateTime startDate,
        [FromQuery] TimeSpan startTime, // Güncellendi
        [FromQuery] DateTime endDate,
        [FromQuery] TimeSpan endTime) // Güncellendi
    {
        // Tarih ve saatleri birleştirerek tam DateTime nesnelerini oluştur
        var startFilter = startDate.Date.Add(startTime);
        var endFilter = endDate.Date.Add(endTime);

        if (startFilter >= endFilter)
        {
            return BadRequest("Başlangıç tarih ve saati, bitiş tarih ve saatinden küçük olmalıdır.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var report = new DailyCashFlowReportDto();

        // --------------------------------------------------------------
        // 1. Müşteri Tahsilatları (PaymentsController) - GİRİŞ
        // --------------------------------------------------------------
        var customerPayments = await _context.Payments
            .Where(p => p.UserId == currentUserId &&
                        p.PaymentDate >= startFilter &&
                        p.PaymentDate < endFilter &&
                        p.SaleId == null)
            .Include(p => p.PaymentType) // PaymentType navigasyon özelliği üzerinden isim almak için eklendi
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
            .Include(t => t.PaymentType)
            .Where(t => t.Company.UserId == currentUserId &&
                        t.Type == MyPos.Domain.Entities.TransactionType.Payment &&
                        t.TransactionDate >= startFilter &&
                        t.TransactionDate < endFilter)
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
                        i.Date >= startFilter &&
                        i.Date < endFilter)
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
                        e.Date >= startFilter &&
                        e.Date < endFilter)
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

        // Satış özetini al (Tarih parametrelerini kullanarak)
        // Burada GetHistoricalSalesTotals metodu çağrılırken de AYNI start/end tarih/saat değerleri gönderilmelidir.
        var historicalSalesSummaryResult = await GetHistoricalSalesTotals(startDate, startTime, endDate, endTime);

        if (historicalSalesSummaryResult.Result is OkObjectResult okResult && okResult.Value is DailySummaryDto salesDto)
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

    // =================================================================
    // GET: api/historicalreport/comprehensive-summary
    // =================================================================

    /// <summary>
    /// Belirtilen tarih aralığına ait Kapsamlı Özet (Kar, Ciro, Kasa).
    /// </summary>
    /// <param name="startDate">Rapor başlangıç tarihi</param>
    /// <param name="startTime">Rapor başlangıç saati</param>
    /// <param name="endDate">Rapor bitiş tarihi</param>
    /// <param name="endTime">Rapor bitiş saati</param>
    [HttpGet("comprehensive-summary")]
    public async Task<ActionResult<ComprehensiveDailySummaryDto>> GetComprehensiveHistoricalSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] TimeSpan startTime, // Güncellendi
        [FromQuery] DateTime endDate,
        [FromQuery] TimeSpan endTime) // Güncellendi
    {
        // Tarih ve saatleri birleştirerek tam DateTime nesnelerini oluştur
        var startFilter = startDate.Date.Add(startTime);
        var endFilter = endDate.Date.Add(endTime);

        if (startFilter >= endFilter)
        {
            return BadRequest("Başlangıç tarih ve saati, bitiş tarih ve saatinden küçük olmalıdır.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Tüm İlgili Verilerin Çekilmesi

        // Satışlar
        var sales = await _context.Sales
            .Where(s => s.UserId == currentUserId && s.SaleDate >= startFilter && s.SaleDate < endFilter)
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
            .ToListAsync();

        var incomes = await _context.Incomes
            .Where(i => i.UserId == currentUserId && i.Date >= startFilter && i.Date < endFilter)
            .ToListAsync();

        var expenses = await _context.Expenses
            .Where(e => e.UserId == currentUserId && e.Date >= startFilter && e.Date < endFilter)
            .ToListAsync();

        // Müşteri Ödemeleri
        var customerPayments = await _context.Payments
            .Where(p => p.UserId == currentUserId && p.PaymentDate >= startFilter && p.PaymentDate < endFilter)
            .Include(p => p.PaymentType)
            .ToListAsync();

        var companyTransactions = await _context.CompanyTransactions
            .Where(ct => ct.Company.UserId == currentUserId &&
                         ct.Type == TransactionType.Payment &&
                         ct.TransactionDate >= startFilter && ct.TransactionDate < endFilter)
            .ToListAsync();

        var cashPurchaseInvoices = await _context.PurchaseInvoices
            .Where(pi => pi.UserId == currentUserId && pi.InvoiceDate >= startFilter && pi.InvoiceDate < endFilter)
            .Include(pi => pi.PaymentType)
            .ToListAsync();

        // =================================================================
        // 2. Hesaplamalar (DailyReportController'daki hesaplamalar birebir korundu)
        // =================================================================

        var totalSales = sales.Sum(s => s.TotalAmount);

        var cashSales = sales
            .Where(s => s.PaymentType == "NAKİT")
            .Sum(s => s.TotalAmount);

        var posSales = sales
            .Where(s => s.PaymentType == "POS" || s.PaymentType == "KREDİ KARTI")
            .Sum(s => s.TotalAmount);

        var cashCustomerPayments = customerPayments
            .Where(p => p.PaymentType != null && p.PaymentType.Name == "NAKİT")
            .Sum(p => p.Amount);

        var posCustomerPayments = customerPayments
            .Where(p => p.PaymentType != null && p.PaymentType.Name != "NAKİT")
            .Sum(p => p.Amount);

        var cashIncomes = incomes
            .Where(i => i.PaymentType == "NAKİT")
            .Sum(i => i.Amount);

        var cashCompanyPayments = companyTransactions
            .Where(ct => ct.PaymentType != null && ct.PaymentType.Name.Equals("NAKİT", StringComparison.OrdinalIgnoreCase))
            .Sum(ct => ct.Amount);

        var cashExpenses = expenses
            .Where(e => e.PaymentType == "NAKİT")
            .Sum(e => e.Amount);

        var cashPurchaseInvoiceTotal = cashPurchaseInvoices
            .Where(pi => pi.PaymentType != null && pi.PaymentType.Name == "NAKİT")
            .Sum(pi => pi.GrandTotal);

        decimal grossProfit = 0;
        decimal totalCostOfGoodsSold = 0;

        foreach (var sale in sales)
        {
            foreach (var item in sale.SaleItems)
            {
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
        var totalIncomes = incomes.Sum(i => i.Amount);
        var totalExpenses = expenses.Sum(e => e.Amount);

        result.RevenueSummary.TotalSalesRevenue = totalSales;
        result.RevenueSummary.CustomerPayments = cashCustomerPayments + posCustomerPayments;
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

    // =================================================================
    // GET: api/historicalreport/customer-payments
    // =================================================================

    /// <summary>
    /// Belirtilen tarih aralığına ait Müşteri Tahsilatlarını detaylı listeler.
    /// </summary>
    /// <param name="startDate">Rapor başlangıç tarihi</param>
    /// <param name="startTime">Rapor başlangıç saati</param>
    /// <param name="endDate">Rapor bitiş tarihi</param>
    /// <param name="endTime">Rapor bitiş saati</param>
    [HttpGet("customer-payments")]
    public async Task<ActionResult<IEnumerable<DailyCustomerPaymentDetailDto>>> GetHistoricalCustomerPayments(
        [FromQuery] DateTime startDate,
        [FromQuery] TimeSpan startTime, // Güncellendi
        [FromQuery] DateTime endDate,
        [FromQuery] TimeSpan endTime) // Güncellendi
    {
        // Tarih ve saatleri birleştirerek tam DateTime nesnelerini oluştur
        var startLocalTime = startDate.Date.Add(startTime);
        var endLocalTime = endDate.Date.Add(endTime);

        if (startLocalTime >= endLocalTime)
        {
            return BadRequest("Başlangıç tarih ve saati, bitiş tarih ve saatinden küçük olmalıdır.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Orijinalindeki gibi UTC saatleri kullanarak filtreleme (Eğer veritabanı UTC tutuyorsa)
        // startFilter ve endFilter'ı burada UTC'ye çeviriyoruz, diğer metotlarda ise yerel zaman kullanılıyordu.
        // Bu tutarsızlığı korumak için sadece bu metotta ToUniversalTime() kullanıldı.
        var startFilter = startLocalTime.ToUniversalTime();
        var endFilter = endLocalTime.ToUniversalTime();

        // 1. O Güne Ait Borç Tahsilatlarını Çek
        var customerPayments = await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.PaymentType)
            .Where(p => p.UserId == currentUserId &&
                        p.PaymentDate >= startFilter &&
                        p.PaymentDate < endFilter &&
                        p.SaleId == null)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new DailyCustomerPaymentDetailDto
            {
                Id = p.Id,
                CustomerName = p.Customer.CustomerName,
                Amount = p.Amount,
                PaymentType = p.PaymentType.Name,
                Note = p.Note,
                Time = p.PaymentDate.ToLocalTime().ToString("HH:mm")
            })
            .ToListAsync();

        return Ok(customerPayments);
    }

    // =================================================================
    // GET: api/historicalreport/company-transactions
    // =================================================================

    /// <summary>
    /// Belirtilen tarih aralığına ait Firmalara göre gruplanmış Borç ve Ödeme işlemlerini listeler.
    /// </summary>
    /// <param name="startDate">Rapor başlangıç tarihi</param>
    /// <param name="startTime">Rapor başlangıç saati</param>
    /// <param name="endDate">Rapor bitiş tarihi</param>
    /// <param name="endTime">Rapor bitiş saati</param>
    [HttpGet("company-transactions")]
    public async Task<ActionResult<IEnumerable<DailyCompanyTransactionDto>>> GetHistoricalCompanyTransactions(
        [FromQuery] DateTime startDate,
        [FromQuery] TimeSpan startTime, // Güncellendi
        [FromQuery] DateTime endDate,
        [FromQuery] TimeSpan endTime) // Güncellendi
    {
        // Tarih ve saatleri birleştirerek tam DateTime nesnelerini oluştur
        var startLocalTime = startDate.Date.Add(startTime);
        var endLocalTime = endDate.Date.Add(endTime);

        if (startLocalTime >= endLocalTime)
        {
            return BadRequest("Başlangıç tarih ve saati, bitiş tarih ve saatinden küçük olmalıdır.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Orijinalindeki gibi UTC saatleri kullanarak filtreleme
        var startFilter = startLocalTime.ToUniversalTime();
        var endFilter = endLocalTime.ToUniversalTime();

        // 1. O Tarih Aralığına Ait Tüm Firma İşlemlerini Çek
        var dailyTransactions = await _context.CompanyTransactions
            .Include(ct => ct.Company)
            .Where(ct => ct.Company.UserId == currentUserId &&
                         ct.TransactionDate >= startFilter &&
                         ct.TransactionDate < endFilter &&
                         (ct.Type == MyPos.Domain.Entities.TransactionType.Debt ||
                          ct.Type == MyPos.Domain.Entities.TransactionType.Payment))
            .ToListAsync();

        // 2. Firmaya göre grupla ve Borç/Ödeme toplamlarını hesapla
        var groupedTransactions = dailyTransactions
            .GroupBy(ct => ct.CompanyId)
            .Select(g => new DailyCompanyTransactionDto
            {
                CompanyId = g.Key,
                CompanyName = g.First().Company.Name,
                TotalDebt = g.Where(ct => ct.Type == MyPos.Domain.Entities.TransactionType.Debt)
                             .Sum(ct => ct.Amount),
                TotalPayment = g.Where(ct => ct.Type == MyPos.Domain.Entities.TransactionType.Payment)
                                 .Sum(ct => ct.Amount)
            })
            .OrderBy(d => d.CompanyName)
            .ToList();

        return Ok(groupedTransactions);
    }
}