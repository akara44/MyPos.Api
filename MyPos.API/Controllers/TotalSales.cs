using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
}