using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MyPos.Application.Dtos.Reports;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProductReportingController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public ProductReportingController(MyPosDbContext context)
    {
        _context = context;
    }

    // POST yerine GET kullanıldı ve Body yerine Query Parametreleri eklendi
    // Örnek Kullanım: /api/ProductReporting/sales-report?startDate=2024-01-01&endDate=2024-01-31
    [HttpGet("sales-report")]
    public async Task<IActionResult> GetProductSalesReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Tarih aralığı belirleme (Varsayılan: Bugün)
        var start = (startDate ?? DateTime.Today).Date;
        // Bitiş tarihi, o günün sonuna ayarlanır (+1 gün ve -1 milisaniye)
        var end = (endDate ?? DateTime.Today).Date.AddDays(1).AddMilliseconds(-1);

        if (start > end)
        {
            return BadRequest("Başlangıç tarihi, bitiş tarihinden sonra olamaz.");
        }

        // 1. İlgili tarihler arasındaki tamamlanmış satışları bul
        var relevantSales = _context.Sales
            .Where(s => s.UserId == currentUserId && s.IsCompleted == true)
            .Where(s => s.SaleDate >= start && s.SaleDate <= end)
            .Select(s => s.SaleId);

        // 2. Bu satışlara ait satış kalemlerini (SaleItem) ProductId bazında grupla
        var reportData = await _context.SaleItems
            .Where(si => relevantSales.Contains(si.SaleId))
            .GroupBy(si => si.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalQuantitySold = g.Sum(si => si.Quantity),
                TotalAmount = g.Sum(si => si.TotalPrice)
            })
            .ToListAsync();

        // Eğer rapor verisi yoksa boş bir liste döndür
        if (!reportData.Any())
        {
            return Ok(new List<ProductSaleReportDto>());
        }

        // 3. Product'ları ve Satış İstatistiklerini Birleştir
        var productIds = reportData.Select(d => d.ProductId).ToList();

        // Product'ları (Stok, Alış Fiyatı ve diğer bilgiler için) tek seferde çek
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.UserId == currentUserId)
            .ToDictionaryAsync(p => p.Id);

        var reportList = new List<ProductSaleReportDto>();

        foreach (var data in reportData)
        {
            if (products.TryGetValue(data.ProductId, out var product))
            {
                // Satış Miktarı, Fiyat, Kar/Zarar Hesaplamaları
                var avgSalePrice = data.TotalAmount / data.TotalQuantitySold;
                var totalPurchaseCost = data.TotalQuantitySold * product.PurchasePrice; // Ürün Entity'deki Alış Fiyatı (PurchasePrice) kullanıldı
                var totalProfitLoss = data.TotalAmount - totalPurchaseCost;
                var avgUnitProfit = totalProfitLoss / data.TotalQuantitySold;

                reportList.Add(new ProductSaleReportDto
                {
                    ProductBarcode = product.Barcode,
                    ProductName = product.Name,
                    TotalQuantitySold = data.TotalQuantitySold,
                    RemainingStock = product.Stock,
                    AvgPurchasePrice = product.PurchasePrice,
                    AvgSalePrice = avgSalePrice,
                    AvgUnitProfit = avgUnitProfit,
                    TotalAmount = data.TotalAmount,
                    TotalProfitLoss = totalProfitLoss
                });
            }
        }

        return Ok(reportList);
    }

    // POST yerine GET kullanıldı ve Body yerine Query Parametreleri eklendi
    // Örnek Kullanım: /api/ProductReporting/report-totals?startDate=2024-01-01&endDate=2024-01-31
    [HttpGet("report-totals")]
    public async Task<IActionResult> GetProductReportTotals(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Tarih aralığı belirleme (Varsayılan: Bugün)
        var start = (startDate ?? DateTime.Today).Date;
        // Bitiş tarihi, o günün sonuna ayarlanır (+1 gün ve -1 milisaniye)
        var end = (endDate ?? DateTime.Today).Date.AddDays(1).AddMilliseconds(-1);

        if (start > end)
        {
            return BadRequest("Başlangıç tarihi, bitiş tarihinden sonra olamaz.");
        }

        // 1. İlgili tarihler arasındaki tamamlanmış satışları bul
        var relevantSales = _context.Sales
            .Where(s => s.UserId == currentUserId && s.IsCompleted == true)
            .Where(s => s.SaleDate >= start && s.SaleDate <= end)
            .Select(s => s.SaleId);

        // 2. Bu satışlara ait satış kalemlerini (SaleItem) ProductId bazında grupla ve toplamları hesapla
        var reportData = await _context.SaleItems
            .Where(si => relevantSales.Contains(si.SaleId))
            .GroupBy(si => si.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalQuantitySold = g.Sum(si => si.Quantity),
                TotalAmount = g.Sum(si => si.TotalPrice)
            })
            .ToListAsync();

        // Rapor verisi yoksa 0'larla boş bir sonuç döndür
        if (!reportData.Any())
        {
            return Ok(new ProductReportTotalsDto
            {
                TotalQuantity = 0,
                TotalAmount = 0,
                TotalProfitLoss = 0
            });
        }

        // 3. Product'ları (Alış Fiyatı için) tek seferde çek
        var productIds = reportData.Select(d => d.ProductId).ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.UserId == currentUserId)
            .ToDictionaryAsync(p => p.Id);

        decimal grandTotalQuantity = 0;
        decimal grandTotalAmount = 0;
        decimal grandTotalProfitLoss = 0;

        foreach (var data in reportData)
        {
            if (products.TryGetValue(data.ProductId, out var product))
            {
                // Kar/Zarar Hesaplaması
                var totalPurchaseCost = data.TotalQuantitySold * product.PurchasePrice;
                var totalProfitLoss = data.TotalAmount - totalPurchaseCost;

                // Genel Toplamları Güncelle
                grandTotalQuantity += data.TotalQuantitySold;
                grandTotalAmount += data.TotalAmount;
                grandTotalProfitLoss += totalProfitLoss;
            }
        }

        // 4. Toplam Sonucu DTO olarak döndür
        return Ok(new ProductReportTotalsDto
        {
            TotalQuantity = grandTotalQuantity,
            TotalAmount = grandTotalAmount,
            TotalProfitLoss = grandTotalProfitLoss
        });
    }
}