using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockTransactionController : ControllerBase
    {
        private readonly MyPosDbContext _context;

        public StockTransactionController(MyPosDbContext context)
        {
            _context = context;
        }

        // Raporu getiren ana metod
        [HttpGet("report")]
        public async Task<IActionResult> GetStockReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? productId,          // Ürün Seçimi
            [FromQuery] int? companyId,          // Firma Seçimi (Sadece IN hareketleri için geçerli)
            [FromQuery] string? reportType,      // Raporlama Türü: "IN", "OUT", "ALL"
            [FromQuery] bool? onlyInactive      // Yalnızca etkisiz işlemler (Eğer 'StockTransaction' entity'sinde 'IsActive' gibi bir alan varsa kullanılır)
        )
        {
            if (!HasPermission("StockReportView")) // Rapor görüntüleme yetkisi kontrolü
            {
                return StatusCode(403, new { message = "Stok hareket raporu görüntüleme yetkiniz yok." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Başlangıç ve bitiş tarihlerini filtrelemek için temel sorgu
            var query = _context.StockTransaction
                .Include(st => st.Product)
                .Where(st => st.UserId == currentUserId && st.Date.Date >= startDate.Date && st.Date.Date <= endDate.Date);

            // 1. Ürün Seçimi Filtresi
            if (productId.HasValue && productId.Value > 0)
            {
                query = query.Where(st => st.ProductId == productId.Value);
            }

            // 2. Raporlama Türü Filtresi
            // "IN" (Giriş) veya "OUT" (Çıkış) filtrelemesi
            if (!string.IsNullOrEmpty(reportType) && reportType.ToUpper() != "ALL")
            {
                query = query.Where(st => st.TransactionType == reportType.ToUpper());
            }

            // 3. Firma Seçimi Filtresi (Firma sadece IN hareketlerinde ve 'Reason' alanında PurchaseInvoiceId tutuluyorsa geçerlidir)
            if (companyId.HasValue && companyId.Value > 0)
            {
                // Bu filtre için karmaşık bir sorgu gereklidir çünkü CompanyId StockTransaction tablosunda doğrudan bulunmaz.
                // Satın Alma Faturası (PurchaseInvoice) üzerinden CompanyId'ye ulaşmalıyız.
                // Bu, veritabanı performansını artırmak için ayrı bir sorgu olarak ele alınabilir.
                // Basitleştirilmiş çözüm: Yalnızca IN (Giriş) işlemleri için geçerli olduğunu varsayıyoruz.

                var invoiceIdsForCompany = await _context.PurchaseInvoices
                    .Where(pi => pi.CompanyId == companyId.Value && pi.UserId == currentUserId)
                    .Select(pi => pi.Id.ToString())
                    .ToListAsync();

                // 'Reason' alanı "PurchaseInvoice:{Id}" formatında tutulduğu için bu şekilde filtreleme yapabiliriz.
                query = query.Where(st => st.TransactionType == "IN" &&
                                          invoiceIdsForCompany.Any(id => st.Reason.Contains($"PurchaseInvoice:{id}")));
            }

            // Not: 'onlyInactive' filtrelemesi için 'StockTransaction' entity'nizde ilgili alan olmalıdır.
            // Bu örnekte bu alan olmadığı varsayılmış, eğer eklenirse buraya mantığı dahil edebilirsiniz.
            // if (onlyInactive.GetValueOrDefault()) { query = query.Where(st => !st.IsActive); }


            var reportData = await query
                .OrderByDescending(st => st.Date)
                .Select(st => new StockTransactionReportDto
                {
                    Date = st.Date,
                    TransactionType = st.TransactionType == "IN" ? "Giriş" : "Çıkış",
                    Reason = st.Reason, // 'Sale:{id}', 'PurchaseInvoice:{id}', 'First Stock Entry', vb.
                    ProductName = st.Product.Name,
                    Barcode = st.Product.Barcode,
                    Quantity = st.QuantityChange,
                    BalanceAfter = st.BalanceAfter
                            })
                .ToListAsync();

            // Eğer daha fazla detaya (Firma Adı, Müşteri Adı, Ödeme Tipi, Birim Fiyat, Tutar) ihtiyaç varsa,
            // 'reportData' üzerinde ek işlem/sorgu yapmak gerekir.
            // Örn: Her bir "OUT" işlemi için ilgili 'SaleItem' ve 'Sale' tablosundan detayları çekmek.

            var finalReport = await AttachExtraDetails(reportData, currentUserId);


            return Ok(finalReport);
        }

        // Yardımcı Yetki Kontrol Metodu
        private bool HasPermission(string claimType)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole == "Admin")
            {
                return true;
            }
            var claimValue = User.FindFirstValue(claimType);
            if (bool.TryParse(claimValue, out bool hasPermission) && hasPermission)
            {
                return true;
            }
            return false;
        }

        // Satış ve Alış Detaylarını getiren yardımcı metot
        // Bu metot, her bir stok hareketine karşılık gelen satış veya alış faturası detaylarını (firma/müşteri adı, tutar, birim fiyat) eklemek için kullanılır.
        // Performans açısından dikkatli kullanılmalıdır.
        private async Task<List<StockTransactionReportDto>> AttachExtraDetails(List<StockTransactionReportDto> reportData, string currentUserId)
        {
            var saleIds = reportData.Where(r => r.ReferenceType == "Sale" && r.ReferenceId != null)
                                    .Select(r => int.Parse(r.ReferenceId!)).ToList();
            var purchaseInvoiceIds = reportData.Where(r => r.ReferenceType == "PurchaseInvoice" && r.ReferenceId != null)
                                            .Select(r => int.Parse(r.ReferenceId!)).ToList();

            // İlgili Satış ve Satın Alma Faturalarını toplu çekme
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Where(s => saleIds.Contains(s.SaleId) && s.UserId == currentUserId)
                .ToDictionaryAsync(s => s.SaleId);

            var purchaseInvoices = await _context.PurchaseInvoices
                .Include(pi => pi.Company)
                .Where(pi => purchaseInvoiceIds.Contains(pi.Id) && pi.UserId == currentUserId)
                .ToDictionaryAsync(pi => pi.Id);

            // Rapor verisine detayları ekleme
            foreach (var item in reportData)
            {
                if (item.ReferenceType == "Sale" && item.ReferenceId != null && int.TryParse(item.ReferenceId, out int saleId))
                {
                    if (sales.TryGetValue(saleId, out var sale))
                    {
                        item.CompanyNameOrCustomerName = sale.Customer?.CustomerName ?? "Anonim Müşteri";
                        item.PaymentType = sale.PaymentType;
                        item.TotalAmount = sale.TotalAmount;

                        // Satış Kalemi Birim Fiyatını bulmak
                        var saleItem = await _context.SaleItems
                            .FirstOrDefaultAsync(si => si.SaleId == saleId && si.ProductName == item.ProductName);
                        item.UnitPrice = saleItem?.UnitPrice ?? 0;
                    }
                }
                else if (item.ReferenceType == "PurchaseInvoice" && item.ReferenceId != null && int.TryParse(item.ReferenceId, out int invoiceId))
                {
                    if (purchaseInvoices.TryGetValue(invoiceId, out var invoice))
                    {
                        item.CompanyNameOrCustomerName = invoice.Company?.Name ?? "Bilinmeyen Firma";
                        item.PaymentType = invoice.PaymentType?.Name; // PaymentType Navigation Property'si eksik olabilir.
                        item.TotalAmount = invoice.GrandTotal;

                        // Satın Alma Kalemi Birim Fiyatını bulmak
                        var purchaseItem = await _context.PurchaseInvoiceItems
                            .FirstOrDefaultAsync(pi => pi.PurchaseInvoiceId == invoiceId && pi.ProductName == item.ProductName);
                        item.UnitPrice = purchaseItem?.UnitPrice ?? 0;
                    }
                }
            }

            return reportData;
        }

    }
}