using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public DashboardController(MyPosDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tamamlanmış Satış metriklerini ADET bazında hesaplar ve karşılaştırma oranlarını döndürür.
    /// </summary>
    [HttpGet("sales-metrics-count")]
    public async Task<IActionResult> GetSalesMetricsByCount()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // UTC kullanımı tarih hesaplamalarında tutarlılık sağlar.
        var now = DateTime.UtcNow;

        // --- 1. Gerekli Tarih Aralıklarını Belirle ---

        var todayStartUtc = now.Date;
        var yesterdayStartUtc = todayStartUtc.AddDays(-1);
        var lastWeekTodayStartUtc = todayStartUtc.AddDays(-7);
        var thisMonthStartUtc = new DateTime(now.Year, now.Month, 1);
        var lastMonthStartUtc = thisMonthStartUtc.AddMonths(-1);
        var lastMonthEndUtc = thisMonthStartUtc;

        // --- 2. Satış Verilerini Çekme Fonksiyonu (ADET/SAYI için) ---

        // Belirli bir aralıktaki TAMAMLANMIŞ satış ADEDİNİ hesaplayan yardımcı fonksiyon
        async Task<int> GetTotalSalesCount(DateTime startDate, DateTime endDate)
        {
            var totalCount = await _context.Sales // *** DEĞİŞİKLİK BURADA: Artık Debts değil, Sales tablosu kullanılıyor ***
                .Where(s => s.UserId == currentUserId &&
                            s.IsCompleted == true && // *** DEĞİŞİKLİK BURADA: Sadece tamamlanan satışlar sayılıyor ***
                            s.SaleDate >= startDate && s.SaleDate < endDate)
                .CountAsync();
            return totalCount;
        }

        // --- 3. Satış Adetlerini Hesapla ---

        // Bugün (Şu ana kadar)
        var todaySalesCount = await GetTotalSalesCount(todayStartUtc, now);

        // Geçen hafta bugün (Şu ana kadar olan aynı dilim)
        var lastWeekTodaySalesCount = await GetTotalSalesCount(lastWeekTodayStartUtc, lastWeekTodayStartUtc.AddHours(now.Hour).AddMinutes(now.Minute));

        // Dün (Tam gün)
        var yesterdaySalesCount = await GetTotalSalesCount(yesterdayStartUtc, todayStartUtc);

        // Bu Ay
        var thisMonthSalesCount = await GetTotalSalesCount(thisMonthStartUtc, now);

        // Geçen Ay (Tam ay)
        var lastMonthSalesCount = await GetTotalSalesCount(lastMonthStartUtc, lastMonthEndUtc);

        // --- 4. Oranları Hesapla (Adet/Sayılar üzerinden) ---

        // Oran Hesaplama Yardımcı Fonksiyonu: Yeni / Eski
        string CalculatePercentageChange(int newCount, int oldCount)
        {
            if (oldCount == 0)
            {
                // Yeni değer 0'dan büyükse "+%100 (Yeni)", 0 ise "%0"
                return newCount > 0 ? "+%100 (Yeni)" : "%0";
            }

            var change = (decimal)newCount - (decimal)oldCount;
            var percentage = (change / (decimal)oldCount) * 100;

            // F1 formatı bir ondalık basamak gösterir (örn: +%12.5)
            return $"{((percentage > 0) ? "+" : "")}{percentage:F1}%";
        }

        // Oranlar
        var todayVsLastWeekRatio = CalculatePercentageChange(todaySalesCount, lastWeekTodaySalesCount);
        var todayVsYesterdayRatio = CalculatePercentageChange(todaySalesCount, yesterdaySalesCount);
        var thisMonthVsLastMonthRatio = CalculatePercentageChange(thisMonthSalesCount, lastMonthSalesCount);

        // --- 5. Sonucu Döndür ---

        var result = new
        {
            // Bugün
            TodaySales = new
            {
                Total = todaySalesCount, // Artık tamamlanmış satış adeti döndürüyor
                Ratio = todayVsLastWeekRatio,
                RatioDesc = "Geçen Hafta Bugün"
            },

            // Bu Ay
            ThisMonthSales = new
            {
                Total = thisMonthSalesCount, // Artık tamamlanmış satış adeti döndürüyor
                Ratio = thisMonthVsLastMonthRatio,
                RatioDesc = "Geçen Ay"
            },

            // Ek Bilgiler (Oranlar)
            ComparisonRatios = new
            {
                TodayVsYesterday = todayVsYesterdayRatio,
                TodayVsLastWeek = todayVsLastWeekRatio,
                ThisMonthVsLastMonth = thisMonthVsLastMonthRatio
            }
        };

        return Ok(result);
    }
    [HttpGet("net-profit-metrics")]
    public async Task<IActionResult> GetNetProfitMetrics()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Gerekli using'ler: System.Linq, System.Security.Claims (Zaten var olmalı)
        // Eğer TransactionType Enum hatası devam ederse: using MyPos.Domain.Enums; gibi bir ifade eklemeniz gerekebilir.
        var now = DateTime.UtcNow;

        // --- 1. Gerekli Tarih Aralıklarını Belirle ---

        var todayStartUtc = now.Date;
        var yesterdayStartUtc = todayStartUtc.AddDays(-1);
        var thisMonthStartUtc = new DateTime(now.Year, now.Month, 1);
        var lastMonthStartUtc = thisMonthStartUtc.AddMonths(-1);

        // --- 2. Dönemlik Net Kazanç Hesaplamaları ---

        // Net Kazanç Hesaplayıcı Fonksiyon
        Func<DateTime, DateTime, Task<decimal>> CalculateNetProfit =
            async (startDate, endDate) =>
            {
                // A. GELİR AKIŞI
                // 1. Tamamlanmış Satışlar (Nakit/Pos) - Hata düzeltildi: GrandTotal yerine TotalAmount kullanıldı.
                // Satış Entity'nizde TotalAmount alanı var.
                var saleIncomes = await _context.Sales
                    .Where(s => s.UserId == currentUserId && s.IsCompleted && s.SaleDate >= startDate && s.SaleDate < endDate)
                    .SumAsync(s => s.TotalAmount);
                // NOT: Satış miktarınızda (TotalAmount) KDV, İndirim vb. zaten hesaba katılmıştır.

                // 2. Ek Gelirler (IncomeController.cs dosyanıza göre Income entity'sinde Amount alanı var)
                var otherIncomes = await _context.Incomes
                    .Where(i => i.UserId == currentUserId && i.Date >= startDate && i.Date < endDate)
                    .SumAsync(i => i.Amount);

                // 3. Müşteri Ödemeleri (Tahsilatlar) (PaymentsController.cs dosyanıza göre Payment entity'sinde Amount alanı var)
                var customerPayments = await _context.Payments
                    .Where(p => p.UserId == currentUserId && p.PaymentDate >= startDate && p.PaymentDate < endDate)
                    .SumAsync(p => p.Amount);


                var totalIncomeFlow = saleIncomes + otherIncomes + customerPayments;


                // B. GİDER AKIŞI
                // 1. Ek Giderler (ExpenseController.cs dosyanıza göre Expense entity'sinde Amount alanı var)
                var expenses = await _context.Expenses
                    .Where(e => e.UserId == currentUserId && e.Date >= startDate && e.Date < endDate)
                    .SumAsync(e => e.Amount);

                // 2. Alış Faturaları (Gider kabul edilir) 
                // GrandTotal, alış faturasının nihai toplamını temsil ediyor.
                var purchaseInvoices = await _context.PurchaseInvoices
                    .Where(p => p.UserId == currentUserId && p.InvoiceDate >= startDate && p.InvoiceDate < endDate)
                    .SumAsync(p => p.GrandTotal);

                // 3. Firma Ödemeleri (Tedarikçiye Ödeme)
                // Hata düzeltildi: TransactionType yerine Type kullanıldı.
                // Ayrıca 'OUT' olanlar Ödeme kabul edilir (Eğer ödeme işlemi için 'Payment' kullanılıyorsa ona da bakılabilir, ancak CompanyTransactionsController 'Payment' veya 'Debt' kullanıyor)
                var companyPayments = await _context.CompanyTransactions
                    .Where(ct => ct.Company.UserId == currentUserId && ct.Type == TransactionType.Payment && ct.TransactionDate >= startDate && ct.TransactionDate < endDate)
                    .SumAsync(ct => ct.Amount);

                var totalExpenseFlow = expenses + purchaseInvoices + companyPayments;


                return totalIncomeFlow - totalExpenseFlow;
            };

        // --- 3. Hesaplamaları Çalıştır ---

        var todayNetProfit = await CalculateNetProfit(todayStartUtc, now);
        var yesterdayNetProfit = await CalculateNetProfit(yesterdayStartUtc, todayStartUtc);
        var thisMonthNetProfit = await CalculateNetProfit(thisMonthStartUtc, now);
        var lastMonthNetProfit = await CalculateNetProfit(lastMonthStartUtc, thisMonthStartUtc);

        // --- 4. Karşılaştırma Oranları ---

        string CalculatePercentageChange(decimal current, decimal old)
        {
            if (old == 0) return current == 0 ? "0%" : (current > 0 ? "+%Inf" : "-%Inf");
            var change = current - old;
            var percentage = (change / Math.Abs(old)) * 100;

            // F1 formatı bir ondalık basamak gösterir (örn: +%12.5)
            return $"{(percentage > 0 ? "+" : "")}{percentage:F1}%";
        }

        var todayVsYesterdayRatio = CalculatePercentageChange(todayNetProfit, yesterdayNetProfit);
        var thisMonthVsLastMonthRatio = CalculatePercentageChange(thisMonthNetProfit, lastMonthNetProfit);


        // --- 5. Sonucu Döndür ---

        var result = new
        {
            // Bugün
            TodayNetProfit = new
            {
                Total = todayNetProfit,
                Ratio = todayVsYesterdayRatio,
                RatioDesc = "Düne Göre"
            },

            // Bu Ay
            ThisMonthNetProfit = new
            {
                Total = thisMonthNetProfit,
                Ratio = thisMonthVsLastMonthRatio,
                RatioDesc = "Geçen Aya Göre"
            },

            // Ek Bilgiler (Oranlar)
            ComparisonRatios = new
            {
                TodayVsYesterday = todayVsYesterdayRatio,
                ThisMonthVsLastMonth = thisMonthVsLastMonthRatio
            }
        };

        return Ok(result);
    } 
}