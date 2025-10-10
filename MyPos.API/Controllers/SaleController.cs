using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims; // Bu using'i ekle
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Auth'u aç
public class SaleController : ControllerBase
{
    private readonly MyPosDbContext _context;
    private readonly SaleValidator _validator;

    public SaleController(MyPosDbContext context)
    {
        _context = context;
        _validator = new SaleValidator();
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequestDto request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors });

        if (request.DiscountValue.HasValue)
        {
            if (string.IsNullOrEmpty(request.DiscountType))
                return BadRequest("İndirim değeri girildiğinde indirim tipi de belirtilmelidir.");
            if (request.DiscountValue <= 0)
                return BadRequest("İndirim değeri 0'dan büyük olmalıdır.");
            if (request.DiscountType == "PERCENTAGE" && request.DiscountValue > 100)
                return BadRequest("Yüzde indirimi 100'den fazla olamaz.");
        }

        var productIds = request.SaleItems.Select(x => x.ProductId).ToList();
        var products = await _context.Products
                                     .Where(p => productIds.Contains(p.Id) && p.UserId == currentUserId)
                                     .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
        {
            return BadRequest("Satışa eklenen ürünlerden bazıları bulunamadı veya yetkiniz yok.");
        }

        var sale = new Sale
        {
            CustomerId = request.CustomerId,
            SaleDate = DateTime.Now,
            IsCompleted = false,
            SubTotalAmount = 0,
            DiscountAmount = 0,
            MiscellaneousTotal = 0,
            TotalAmount = 0,
            PaymentType = null,
            IsDiscountApplied = false,
            SaleItems = new List<SaleItem>(),
            SaleMiscellaneous = new List<SaleMiscellaneous>(),
            SaleCode = $"SAL-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6)}",
            TotalQuantity = 0,
            UserId = currentUserId
        };

        foreach (var itemDto in request.SaleItems)
        {
            if (!products.TryGetValue(itemDto.ProductId, out var product))
                return NotFound($"Ürün ID'si {itemDto.ProductId} bulunamadı.");
            if (product.Stock < itemDto.Quantity)
                return BadRequest($"Ürün '{product.Name}' için yeterli stok yok. Mevcut stok: {product.Stock}.");

            var saleItem = new SaleItem
            {
                ProductId = itemDto.ProductId,
                ProductName = product.Name,
                Quantity = itemDto.Quantity,
                UnitPrice = product.SalePrice,
                TotalPrice = itemDto.Quantity * product.SalePrice,
                Discount = 0,
                UserId = currentUserId // Düzeltme: UserId eklendi
            };

            sale.SubTotalAmount += saleItem.TotalPrice;
            sale.TotalQuantity += itemDto.Quantity;
            sale.SaleItems.Add(saleItem);
        }

        if (request.DiscountValue.HasValue && !string.IsNullOrEmpty(request.DiscountType))
        {
            decimal discountAmount = 0;
            if (request.DiscountType == "PERCENTAGE")
            {
                discountAmount = sale.SubTotalAmount * (request.DiscountValue.Value / 100);
            }
            else if (request.DiscountType == "AMOUNT")
            {
                if (request.DiscountValue.Value > sale.SubTotalAmount)
                    return BadRequest("İndirim tutarı, ara toplamdan fazla olamaz.");
                discountAmount = request.DiscountValue.Value;
            }
            sale.DiscountType = request.DiscountType;
            sale.DiscountValue = request.DiscountValue.Value;
            sale.DiscountAmount = discountAmount;
            sale.IsDiscountApplied = true;
        }

        if (request.MiscellaneousItems != null && request.MiscellaneousItems.Any())
        {
            foreach (var miscItem in request.MiscellaneousItems)
            {
                var saleMisc = new SaleMiscellaneous
                {
                    Description = miscItem.Description,
                    Amount = miscItem.Amount,
                    CreatedDate = DateTime.Now,
                    UserId = currentUserId // Düzeltme: UserId eklendi
                };
                sale.SaleMiscellaneous.Add(saleMisc);
                sale.MiscellaneousTotal += miscItem.Amount;
            }
        }

        sale.TotalAmount = sale.SubTotalAmount - sale.DiscountAmount + sale.MiscellaneousTotal;

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSaleById), new { saleId = sale.SaleId },
            await GetSaleDetailsDto(sale.SaleId, currentUserId));
    }

    [HttpGet("{saleId}")]
    public async Task<IActionResult> GetSaleById(int saleId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var saleDetailsDto = await GetSaleDetailsDto(saleId, currentUserId); // Yetki kontrolü ekle
        if (saleDetailsDto == null)
            return NotFound("Satış bulunamadı veya yetkiniz yok.");

        return Ok(saleDetailsDto);
    }

    // Private metot yetki kontrolü için güncellendi
    private async Task<SaleDetailsDto?> GetSaleDetailsDto(int saleId, string userId)
    {
        var sale = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .Include(s => s.SaleMiscellaneous)
            .FirstOrDefaultAsync(s => s.SaleId == saleId && s.UserId == userId); // Yetki kontrolü

        if (sale == null)
            return null;

        return new SaleDetailsDto
        {
            SaleId = sale.SaleId,
            CustomerId = sale.CustomerId,
            CustomerName = sale.Customer?.CustomerName,
            SubTotalAmount = sale.SubTotalAmount,
            DiscountAmount = sale.DiscountAmount,
            DiscountType = sale.DiscountType,
            DiscountValue = sale.DiscountValue,
            MiscellaneousTotal = sale.MiscellaneousTotal,
            TotalAmount = sale.TotalAmount,
            PaymentType = sale.PaymentType,
            SaleDate = sale.SaleDate,
            IsCompleted = sale.IsCompleted,
            IsDiscountApplied = sale.IsDiscountApplied,
            SaleCode = sale.SaleCode,
            TotalQuantity = sale.TotalQuantity,
            SaleItems = sale.SaleItems.Select(si => new SaleItemDetailsDto

            {
                SaleItemId = si.SaleItemId,
                ProductId = si.ProductId,
                ProductName = si.ProductName,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TotalPrice = si.TotalPrice,
                Discount = si.Discount
            }).ToList(),
            SaleMiscellaneous = sale.SaleMiscellaneous.Select(sm => new SaleMiscellaneousDto
            {
                SaleMiscellaneousId = sm.SaleMiscellaneousId,
                Description = sm.Description,
                Amount = sm.Amount,
                CreatedDate = sm.CreatedDate
            }).ToList()
        };
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllSales()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var sales = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .Include(s => s.SaleMiscellaneous)
            .Where(s => s.UserId == currentUserId) // Sadece mevcut kullanıcıya ait satışları getir
            .ToListAsync();

        var salesDto = sales.Select(s => new SaleDetailsDto
        {
            SaleId = s.SaleId,
            CustomerId = s.CustomerId,
            CustomerName = s.Customer?.CustomerName,
            SubTotalAmount = s.SubTotalAmount,
            DiscountAmount = s.DiscountAmount,
            DiscountType = s.DiscountType,
            DiscountValue = s.DiscountValue,
            MiscellaneousTotal = s.MiscellaneousTotal,
            TotalAmount = s.TotalAmount,
            PaymentType = s.PaymentType,
            SaleDate = s.SaleDate,
            IsCompleted = s.IsCompleted,
            IsDiscountApplied = s.IsDiscountApplied,
            SaleCode = s.SaleCode,
            TotalQuantity = s.TotalQuantity,
            SaleItems = s.SaleItems.Select(si => new SaleItemDetailsDto
            {
                SaleItemId = si.SaleItemId,
                ProductId = si.ProductId,
                ProductName = si.ProductName,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TotalPrice = si.TotalPrice,
                Discount = si.Discount
            }).ToList(),
            SaleMiscellaneous = s.SaleMiscellaneous.Select(sm => new SaleMiscellaneousDto
            {
                SaleMiscellaneousId = sm.SaleMiscellaneousId,
                Description = sm.Description,
                Amount = sm.Amount,
                CreatedDate = sm.CreatedDate
            }).ToList()
        }).ToList();
        return Ok(salesDto);
    }

    // SaleController.cs

    // SaleController.cs

    [HttpPost("{saleId}/finalize")]
    public async Task<IActionResult> FinalizeSale(int saleId, [FromBody] FinalizeSaleRequestDto request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .FirstOrDefaultAsync(s => s.SaleId == saleId && s.UserId == currentUserId);

            if (sale == null)
                return NotFound($"Satış ID'si {saleId} bulunamadı veya yetkiniz yok.");

            if (sale.IsCompleted)
                return BadRequest("Bu satış zaten tamamlanmış.");

            var paymentType = await _context.PaymentTypes.FirstOrDefaultAsync(pt => pt.Id == request.PaymentTypeId && pt.UserId == currentUserId);
            if (paymentType == null)
                return BadRequest("Seçilen ödeme tipi bulunamadı veya yetkiniz yok.");

            sale.PaymentType = paymentType.Name;

            if (sale.CustomerId.HasValue)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId.Value && c.UserId == currentUserId);
                if (customer == null)
                {
                    throw new Exception("Satışa bağlı müşteri bulunamadı.");
                }

                if (paymentType.Name.Equals("Açık Hesap", StringComparison.OrdinalIgnoreCase))
                {
                    // --- MANTIK DEĞİŞİKLİĞİ BAŞLANGIÇ ---

                    // Önce potansiyel yeni bakiyeyi hesaplıyoruz.
                    decimal yeniBakiye = customer.Balance + sale.TotalAmount;

                    // Kontrolü artık 'yeniBakiye' üzerinden yapıyoruz (Eski haline döndü).
                    if (customer.OpenAccountLimit.HasValue && yeniBakiye > customer.OpenAccountLimit.Value)
                    {
                        // Hata mesajını yeni mantığa göre güncelledik.
                        return BadRequest($"Bu satış ile müşterinin toplam borç limiti aşılamaz. Limit: {customer.OpenAccountLimit.Value:F2}, Mevcut Borç: {customer.Balance:F2}, Yeni Borç: {yeniBakiye:F2}");
                    }

                    // Bakiyeyi, kontrol sonrası daha önce hesapladığımız değişkenle güncelliyoruz.
                    customer.Balance = yeniBakiye;
                    _context.Customers.Update(customer);

                    // --- MANTIK DEĞİŞİKLİĞİ BİTİŞ ---
                }
            }

            // Stok düşürme mantığı aynı şekilde devam ediyor...
            var productIds = sale.SaleItems.Select(si => si.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id) && p.UserId == currentUserId)
                .ToDictionaryAsync(p => p.Id);

            foreach (var saleItem in sale.SaleItems)
            {
                if (products.TryGetValue(saleItem.ProductId, out var product))
                {
                    product.Stock -= saleItem.Quantity;
                    _context.StockTransaction.Add(new StockTransaction
                    {
                        ProductId = product.Id,
                        QuantityChange = -saleItem.Quantity,
                        TransactionType = "OUT",
                        Reason = $"Sale:{saleId}",
                        Date = DateTime.Now,
                        BalanceAfter = product.Stock,
                        UserId = currentUserId
                    });
                }
            }

            sale.IsCompleted = true;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Satış başarıyla tamamlandı.",
                saleId = sale.SaleId,
                paymentType = sale.PaymentType,
                totalAmount = sale.TotalAmount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Satış tamamlanırken hata oluştu: " + ex.Message);
        }
    }

    [HttpPost("{saleId}/finalize-split")]
    public async Task<IActionResult> FinalizeSaleSplit(int saleId, [FromBody] SplitPaymentRequestDto request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // DİKKAT: Stok düşürme ve müşteri bakiye güncelleme işlemleri için transaction kullanmak önemli.
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems) // Stok düşürmek için SaleItems dahil edildi.
                .FirstOrDefaultAsync(s => s.SaleId == saleId && s.UserId == currentUserId);

            if (sale == null)
                return NotFound("Satış bulunamadı veya yetkiniz yok.");

            if (sale.IsCompleted)
                return BadRequest("Satış zaten tamamlanmış.");

            var totalSplit = request.SplitPayments.Sum(x => x.Amount);
            if (totalSplit != sale.TotalAmount)
                return BadRequest($"Parçalı ödemelerin toplamı ({totalSplit}) satış tutarına ({sale.TotalAmount}) eşit değil.");

            var paymentTypeIds = request.SplitPayments.Where(sp => sp.PaymentTypeId.HasValue).Select(sp => sp.PaymentTypeId.Value).ToList();
            var paymentTypes = await _context.PaymentTypes
                                             .Where(pt => paymentTypeIds.Contains(pt.Id) && pt.UserId == currentUserId)
                                             .ToDictionaryAsync(pt => pt.Id);

            // Müşteri ve Açık Hesap Kontrolü
            var hasOpenAccountPayment = false;
            foreach (var sp in request.SplitPayments)
            {
                if (paymentTypes.TryGetValue(sp.PaymentTypeId ?? 0, out var pt) && pt.Name.Equals("Açık Hesap", StringComparison.OrdinalIgnoreCase))
                {
                    hasOpenAccountPayment = true;
                    if (!sale.CustomerId.HasValue)
                    {
                        return BadRequest("Açık Hesap ödemesi için satışa bir müşteri atanmalıdır.");
                    }
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId.Value && c.UserId == currentUserId);
                    if (customer == null)
                    {
                        throw new Exception("Satışa bağlı müşteri bulunamadı.");
                    }
                    decimal yeniBakiye = customer.Balance + sp.Amount; // Sadece açık hesap tutarı kadar borç eklenir.
                    if (customer.OpenAccountLimit.HasValue && yeniBakiye > customer.OpenAccountLimit.Value)
                    {
                        return BadRequest($"Bu işlem ile müşterinin borç limiti aşılamaz. Limit: {customer.OpenAccountLimit.Value:F2}, Mevcut Borç: {customer.Balance:F2}, Yeni Borç: {yeniBakiye:F2}");
                    }
                    customer.Balance = yeniBakiye;
                    _context.Customers.Update(customer);
                }
            }

            // Ödeme Kayıtlarını Oluşturma
            foreach (var sp in request.SplitPayments)
            {
                string paymentTypeName = sp.PaymentTypeName;
                if (sp.PaymentTypeId.HasValue && paymentTypes.TryGetValue(sp.PaymentTypeId.Value, out var pt))
                {
                    paymentTypeName = pt.Name;
                }

                _context.Payments.Add(new Payment
                {
                    SaleId = sale.SaleId,
                    CustomerId = sale.CustomerId ?? 0,
                    Amount = sp.Amount,
                    PaymentDate = DateTime.Now,
                    PaymentTypeId = sp.PaymentTypeId ?? 0, // <-- Use PaymentTypeId property
                    Note = paymentTypeName, // Optionally store the name in Note if needed
                    UserId = currentUserId
                });
            }

            // Stok Düşürme
            var productIds = sale.SaleItems.Select(si => si.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id) && p.UserId == currentUserId)
                .ToDictionaryAsync(p => p.Id);

            foreach (var saleItem in sale.SaleItems)
            {
                if (products.TryGetValue(saleItem.ProductId, out var product))
                {
                    if (product.Stock < saleItem.Quantity)
                    {
                        throw new Exception($"'{product.Name}' için yeterli stok yok. İşlem iptal edildi.");
                    }
                    product.Stock -= saleItem.Quantity;
                    _context.StockTransaction.Add(new StockTransaction
                    {
                        ProductId = product.Id,
                        QuantityChange = -saleItem.Quantity,
                        TransactionType = "OUT",
                        Reason = $"Sale:{saleId}",
                        Date = DateTime.Now,
                        BalanceAfter = product.Stock,
                        UserId = currentUserId
                    });
                }
            }

            // Satış Durumunu Güncelle
            sale.PaymentType = "Parçalı"; // <<< DEĞİŞİKLİK BURADA
            sale.IsCompleted = true;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Satış parçalı ödeme ile tamamlandı.",
                totalAmount = sale.TotalAmount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Satış tamamlanırken hata oluştu: " + ex.Message);
        }
    }
    [HttpPost("quick")]
    public async Task<IActionResult> QuickSale([FromBody] QuickSaleRequestDto request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Ödeme Tipini ve Ürünleri Kontrol Et
        var paymentType = await _context.PaymentTypes
            .FirstOrDefaultAsync(pt => pt.Id == request.PaymentTypeId && pt.UserId == currentUserId);

        if (paymentType == null)
            return BadRequest("Seçilen ödeme tipi bulunamadı veya yetkiniz yok.");

        var productIds = request.SaleItems.Select(x => x.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.UserId == currentUserId)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
        {
            return BadRequest("Satışa eklenen ürünlerden bazıları bulunamadı veya yetkiniz yok.");
        }

        // Stok kontrolü yapmadan önce bir işlem (transaction) başlatıyoruz.
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 2. Satış Nesnesini Oluştur ve Hesapla (CreateSale mantığı)
            var sale = new Sale
            {
                CustomerId = request.CustomerId,
                SaleDate = DateTime.Now,
                IsCompleted = true, // Hızlı satış anında tamamlanır.
                SubTotalAmount = 0,
                DiscountAmount = 0, // Hızlı satışta indirim 0 kabul edildi.
                MiscellaneousTotal = 0, // Hızlı satışta ek ücret 0 kabul edildi.
                TotalAmount = 0,
                PaymentType = paymentType.Name,
                IsDiscountApplied = false,
                SaleItems = new List<SaleItem>(),
                SaleMiscellaneous = new List<SaleMiscellaneous>(),
                SaleCode = $"QCK-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6)}",
                TotalQuantity = 0,
                UserId = currentUserId
            };

            // Ürünleri döngüye al, hesaplamaları yap ve stok kontrolü yap
            foreach (var itemDto in request.SaleItems)
            {
                if (!products.TryGetValue(itemDto.ProductId, out var product))
                    throw new Exception($"Ürün ID'si {itemDto.ProductId} bulunamadı."); // Bulunmama kontrolü yukarıda yapıldı, bu daha çok bir güvenlik önlemi.

                if (product.Stock < itemDto.Quantity)
                {
                    // Stok yetersizse işlemi iptal et
                    await transaction.RollbackAsync();
                    return BadRequest($"Ürün '{product.Name}' için yeterli stok yok. Mevcut stok: {product.Stock}.");
                }

                var saleItem = new SaleItem
                {
                    ProductId = itemDto.ProductId,
                    ProductName = product.Name,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.SalePrice,
                    TotalPrice = itemDto.Quantity * product.SalePrice,
                    Discount = 0,
                    UserId = currentUserId
                };

                sale.SubTotalAmount += saleItem.TotalPrice;
                sale.TotalQuantity += itemDto.Quantity;
                sale.SaleItems.Add(saleItem);

                // 3. Stok Güncelleme ve Stok Hareketini Kaydet (FinalizeSale mantığı)
                product.Stock -= saleItem.Quantity;
                _context.StockTransaction.Add(new StockTransaction
                {
                    ProductId = product.Id,
                    QuantityChange = -saleItem.Quantity,
                    TransactionType = "OUT",
                    Reason = $"QuickSale:{sale.SaleCode}",
                    Date = DateTime.Now,
                    BalanceAfter = product.Stock,
                    UserId = currentUserId
                });
                _context.Products.Update(product); // Product'ı da güncelle
            }

            sale.TotalAmount = sale.SubTotalAmount - sale.DiscountAmount + sale.MiscellaneousTotal;

            // 4. Açık Hesap Kontrolü ve Müşteri Bakiye Güncelleme
            if (request.CustomerId.HasValue && paymentType.Name.Equals("Açık Hesap", StringComparison.OrdinalIgnoreCase))
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId.Value && c.UserId == currentUserId);

                if (customer == null)
                {
                    // Müşteri ID'si gönderildi ama bulunamadıysa hata fırlat
                    throw new Exception("Satışa bağlı müşteri bulunamadı.");
                }

                decimal yeniBakiye = customer.Balance + sale.TotalAmount;

                if (customer.OpenAccountLimit.HasValue && yeniBakiye > customer.OpenAccountLimit.Value)
                {
                    // Limit aşımı varsa işlemi geri al ve hata döndür
                    await transaction.RollbackAsync();
                    return BadRequest($"Bu satış ile müşterinin borç limiti aşılamaz. Limit: {customer.OpenAccountLimit.Value:F2}, Mevcut Borç: {customer.Balance:F2}, Yeni Borç: {yeniBakiye:F2}");
                }

                customer.Balance = yeniBakiye;
                _context.Customers.Update(customer);
            }

            // 5. Ödeme Kaydını Ekle (Opsiyonel, ayrı tablo kullanıyorsanız)
            _context.Payments.Add(new Payment
            {
                SaleId = sale.SaleId,
                CustomerId = sale.CustomerId ?? 0,
                Amount = sale.TotalAmount,
                PaymentDate = DateTime.Now,
                PaymentTypeId = request.PaymentTypeId,
                Note = paymentType.Name,
                UserId = currentUserId
            });


            // 6. Satışı Kaydet ve İşlemi Tamamla
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetSaleById), new { saleId = sale.SaleId }, new
            {
                message = "Hızlı satış başarıyla tamamlandı.",
                saleId = sale.SaleId,
                saleCode = sale.SaleCode,
                totalAmount = sale.TotalAmount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // Loglama burada yapılmalı.
            return StatusCode(500, "Hızlı satış tamamlanırken hata oluştu: " + ex.Message);
        }
    }
}


// DTO laı ayırmayı unutma ===!!!    
public class FinalizeSaleRequestDto
{
    [Required]
    public int PaymentTypeId { get; set; } // DB’den PaymentType ID
}
public class SplitPaymentRequestDto
{
    public List<SplitPaymentDto> SplitPayments { get; set; }
}

public class SplitPaymentDto
{
    public string PaymentTypeName { get; set; } // "Nakit", "POS" ya da DB’den gelen isim
    public int? PaymentTypeId { get; set; }      // sadece DB’den gelenler için kullanılır
    public decimal Amount { get; set; }
}
public class QuickSaleItemDto
{
    [Required]
    public int ProductId { get; set; }
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class QuickSaleRequestDto
{
    // Hızlı satışta indirim ve ek ücretler genelde kullanılmaz veya 0 kabul edilir.
    // İhtiyaca göre sonradan eklenebilir.

    [Required]
    public List<QuickSaleItemDto> SaleItems { get; set; }

    [Required]
    public int PaymentTypeId { get; set; } // Hangi ödeme tipiyle kapatılacak (Nakit, Kart vb.)

    public int? CustomerId { get; set; } // Müşteri zorunlu değil (anonim satış olabilir)
}