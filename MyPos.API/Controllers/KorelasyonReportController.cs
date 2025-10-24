using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Reports;
using MyPos.Domain.Entities; // Entity'lerinizin olduğu namespace
using MyPos.Infrastructure.Persistence; // DbContext'inizin olduğu namespace
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class KorelasyonReportController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public KorelasyonReportController(MyPosDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ürün adına göre arama yaparak barkod ve temel bilgileri getirir.
    /// </summary>
    /// <param name="productName">Aranacak ürün adı veya bir kısmı</param>
    [HttpGet("product-search")]
    public async Task<IActionResult> GetProductBarcodeByName([FromQuery] string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return BadRequest("Aranacak ürün adı boş olamaz.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string search = productName.Trim().ToLower();

        // Ürün adı kısmi olarak eşleşen ürünleri getir
        var products = await _context.Products
            .Where(p => p.UserId == currentUserId && p.Name.ToLower().Contains(search))
            .Select(p => new ProductBarcodeDto
            {
                ProductId = p.Id,
                Barcode = p.Barcode,
                Name = p.Name,
                SalePrice = p.SalePrice
            })
            .Take(10) // Performans için ilk 10 sonuç
            .ToListAsync();

        if (!products.Any())
        {
            return NotFound("Aranan kriterlere uygun ürün bulunamadı.");
        }

        return Ok(products);
    }


    /// <summary>
    /// Belirtilen barkoda sahip ürünle birlikte satılan ürünlerin korelasyon raporunu hazırlar.
    /// </summary>
    /// <param name="barcode">Ana ürünün barkodu</param>
    /// <param name="startDate">Rapor başlangıç tarihi (örn: 2025-10-01)</param>
    /// <param name="endDate">Rapor bitiş tarihi (örn: 2025-10-21)</param>
    [HttpGet("product-correlation")]
    public async Task<IActionResult> GetProductCorrelationReport(
    [FromQuery] string barcode,
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return BadRequest("Ürün barkodu zorunludur.");

        // Raporun bitiş saati 24:00 olarak ayarlanır
        endDate = endDate.Date.AddDays(1).AddSeconds(-1);

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Ana Ürünü Bul ve Yetki Kontrolü Yap
        var mainProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.UserId == currentUserId);

        if (mainProduct == null)
            return NotFound($"Barkodu '{barcode}' olan ürün bulunamadı veya yetkiniz yok.");

        // 2. Ana Ürünün Yer Aldığı Tamamlanmış Satışları Belirle (Tarih aralığı dikkate alınarak)
        var relevantSales = await _context.SaleItems
            .Where(si => si.ProductId == mainProduct.Id &&
                         si.Sale.UserId == currentUserId &&
                         si.Sale.IsCompleted == true &&
                         si.Sale.SaleDate >= startDate &&
                         si.Sale.SaleDate <= endDate)
            .Select(si => si.SaleId)
            .Distinct()
            .ToListAsync();

        if (!relevantSales.Any())
        {
            return NotFound("Belirtilen tarih aralığında bu ürünün yer aldığı tamamlanmış bir satış bulunamadı.");
        }

        // 3. İlgili Satışlardaki Tüm Ürünlerin Detaylarını Topla ve Filtrele
        var correlatedSaleItems = await _context.SaleItems
            .Include(si => si.Product) // Kar hesaplamak için CostPrice'a erişim için Product'ı dahil et.
            .Where(si => relevantSales.Contains(si.SaleId) && si.UserId == currentUserId)
            // Yeni Kural: Ana ürünün kendisini (ProductId'sini) rapordan hariç tut
            .Where(si => si.ProductId != mainProduct.Id)
            .GroupBy(si => new { si.ProductId, si.ProductName, si.Product.Barcode, si.Product.PurchasePrice })
            .Select(g => new ProductCorrelationItemDto
            {
                ProductBarcode = g.Key.Barcode,
                ProductName = g.Key.ProductName,
                TotalSaleQuantity = g.Sum(si => si.Quantity),
                TotalAmount = g.Sum(si => si.TotalPrice),
                // Kar hesaplaması: (Toplam Tutar) - (Toplam Satış Adedi * Birim Maliyet)
                TotalProfit = g.Sum(si => si.TotalPrice) - g.Sum(si => si.Quantity) * g.Key.PurchasePrice
            })
            .OrderByDescending(x => x.TotalSaleQuantity)
            .ToListAsync();

        // 4. Korelasyon Kontrolü ve Uyarı Mesajı
        if (!correlatedSaleItems.Any())
        {
            // Ana ürünün satıldığı satışlar var (relevantSales.Any() kontrolü yapıldı), 
            // ancak  2bu satışlarda ana üründen başka ürün yok.
            return Ok(new
            {
                message = $"UYARI: '{mainProduct.Name}' ({mainProduct.Barcode}) ürünü, belirtilen tarih aralığında yalnızca tek başına satılmıştır. Yanında satılan başka bir ürün bulunmamaktadır.",
                CorrelatedProducts = new List<ProductCorrelationItemDto>()
            });
        }

        // 5. Raporu Döndür
        return Ok(correlatedSaleItems);
    }
}