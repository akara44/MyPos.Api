using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using System;
using System.Linq;
using System.Security.Claims; // ClaimTypes.NameIdentifier için
using System.Threading.Tasks;
using System.Collections.Generic; // IEnumerable ve List için

// Temiz mimariye uygun olarak sadece raporlama işlevlerini içerir.
[Route("api/[controller]")]
[ApiController]
[Authorize] // Auth'u aç
public class ReportController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public ReportController(MyPosDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Son 30 gün içindeki tamamlanmış satışların günlük toplam tutarını getirir. (Kayan Pencere)
    /// </summary>
    /// <returns>Günlük satış toplamları listesi</returns>
    [HttpGet("daily-sales-30-days")]
    public async Task<IActionResult> GetDailySalesFor30Days()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Raporun başlangıcı: Bugünün 30 gün öncesi
        var startDate = DateTime.Today.AddDays(-30);

        var dailySales = await _context.Sales
            .Where(s => s.UserId == currentUserId && s.IsCompleted == true && s.SaleDate.Date >= startDate)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalAmount = g.Sum(s => s.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Verilerin olmadığı günler için 0.00 TL ekleyerek 31 günlük (30 gün öncesinden bugüne) 
        // sürekli bir zaman serisi oluşturulur.
        var result = Enumerable.Range(0, 31)
            .Select(offset => startDate.AddDays(offset).Date)
            .GroupJoin(dailySales,
                       date => date,
                       sale => sale.Date,
                       (date, sales) => new
                       {
                           Date = date.ToString("yyyy-MM-dd"),
                           TotalAmount = sales.Sum(s => s.TotalAmount)
                       })
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Son 6 ay içindeki tamamlanmış satışların aylık toplam tutarını getirir. (Kayan Pencere)
    /// </summary>
    /// <returns>Aylık satış toplamları listesi</returns>
    [HttpGet("monthly-sales-6-months")]
    public async Task<IActionResult> GetMonthlySalesFor6Months()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Raporun başlangıcı: Bugünün 6 ay öncesi
        var sixMonthsAgo = DateTime.Today.AddMonths(-6);

        var monthlySales = await _context.Sales
            .Where(s => s.UserId == currentUserId && s.IsCompleted == true && s.SaleDate >= sixMonthsAgo)
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalAmount = g.Sum(s => s.TotalAmount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        // Son 6 ayı ve mevcut ayı (toplam 7 aylık periyot) doldurmak için döngü oluşturulur.
        var result = Enumerable.Range(0, 7)
            .Select(offset => DateTime.Today.AddMonths(-offset))
            .Distinct()
            .OrderBy(d => d)
            .Select(d => new { Year = d.Year, Month = d.Month, MonthName = d.ToString("MMM yyyy") })
            .GroupJoin(monthlySales,
                       d => new { d.Year, d.Month },
                       s => new { s.Year, s.Month },
                       (d, sales) => new
                       {
                           Month = d.MonthName,
                           TotalAmount = sales.Sum(s => s.TotalAmount)
                       })
            .ToList();

        return Ok(result);
    }
}