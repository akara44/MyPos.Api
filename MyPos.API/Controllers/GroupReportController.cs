using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Reports;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Auth'u aç
public class GroupReportController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public GroupReportController(MyPosDbContext context)
    {
        _context = context;
    }

    [HttpGet("group-report")]
    public async Task<IActionResult> GetGroupReport([FromQuery] GroupReportRequestDto request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Başlangıç tarihi için saati 00:00:00 olarak ayarla
        var startDate = request.StartDate.Date;

        // Bitiş tarihi için saati 23:59:59.999 olarak ayarla
        // (Bitiş gününün tamamını dahil etmek için)
        var endDate = request.EndDate.Date.AddDays(1).AddMilliseconds(-1);

        // Kullanılacak verileri veritabanından çekme
        // LINQ sorgusu ile gruplama ve hesaplama
        var groupedReport = await _context.SaleItems
            .Include(si => si.Sale) // SaleItem'dan Sale'e ulaşmak
            .Include(si => si.Product) // SaleItem'dan Product'a ulaşmak
            .ThenInclude(p => p.ProductGroup) // Product'dan ProductGroup'a ulaşmak
            .Where(si => si.Sale.UserId == currentUserId &&
                         si.Sale.IsCompleted && // Sadece tamamlanmış satışları alıyoruz
                         si.Sale.SaleDate >= startDate &&
                         si.Sale.SaleDate <= endDate)
            .GroupBy(si => si.Product.ProductGroup) // ProductGroup'a göre grupla
            .Select(g => new GroupReportItemDto
            {
                // Grup Adı: Eğer grup ID'si yoksa (null) "GRUPLSUZ ÜRÜNLER" olarak adlandır
                GroupName = g.Key != null ? g.Key.Name : "GRUPSUZ ÜRÜNLER",
                // Toplam Miktar: Satış kalemlerindeki Quantity toplamı
                TotalQuantity = g.Sum(si => si.Quantity),
                // Toplam Satış: Satış kalemlerinin toplam fiyatı (TotalPrice)
                TotalSales = g.Sum(si => si.TotalPrice),
                // Toplam Kâr: (Satış Fiyatı - Alış Fiyatı) * Miktar
                TotalProfit = g.Sum(si => g.Key.Id == null
                                          ? g.Sum(si => si.Quantity * (si.Product.SalePrice - si.Product.PurchasePrice))
                                          : si.Quantity * (si.Product.SalePrice - si.Product.PurchasePrice)),
                // Satılan Tekil Ürün Sayısı (Bu gruptan kaç farklı ürün satılmış)
                SoldProductsCount = g.Select(si => si.ProductId).Distinct().Count()
            })
            .OrderByDescending(r => r.TotalSales)
            .ToListAsync();

        return Ok(groupedReport);
    }
}
