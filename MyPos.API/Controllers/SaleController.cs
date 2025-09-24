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

        var sale = await _context.Sales
            .FirstOrDefaultAsync(s => s.SaleId == saleId && s.UserId == currentUserId); // Satış yetki kontrolü
        if (sale == null)
            return NotFound("Satış bulunamadı veya yetkiniz yok.");

        if (sale.IsCompleted)
            return BadRequest("Satış zaten tamamlanmış.");

        var totalSplit = request.SplitPayments.Sum(x => x.Amount);
        if (totalSplit != sale.TotalAmount)
            return BadRequest($"Parçalı ödemelerin toplamı ({totalSplit}) satış tutarına ({sale.TotalAmount}) eşit değil.");

        // Ödeme tiplerini tek sorguda al ve yetkiyi kontrol et
        var paymentTypeIds = request.SplitPayments.Where(sp => sp.PaymentTypeId.HasValue).Select(sp => sp.PaymentTypeId.Value).ToList();
        var paymentTypes = await _context.PaymentTypes
                                         .Where(pt => paymentTypeIds.Contains(pt.Id) && pt.UserId == currentUserId)
                                         .ToDictionaryAsync(pt => pt.Id);

        foreach (var sp in request.SplitPayments)
        {
            string paymentTypeName;
            if (sp.PaymentTypeName == "Nakit" || sp.PaymentTypeName == "POS")
            {
                paymentTypeName = sp.PaymentTypeName;
            }
            else
            {
                if (sp.PaymentTypeId == null)
                    return BadRequest("DB ödeme tipi için PaymentTypeId boş olamaz.");

                if (!paymentTypes.TryGetValue(sp.PaymentTypeId.Value, out var paymentType))
                    return BadRequest($"Geçersiz ödeme tipi: {sp.PaymentTypeId} veya yetkiniz yok.");

                paymentTypeName = paymentType.Name;
            }

            _context.Payments.Add(new Payment
            {
                SaleId = sale.SaleId,
                CustomerId = sale.CustomerId ?? 0,
                Amount = sp.Amount,
                PaymentDate = DateTime.Now,
                PaymentTypeName = paymentTypeName,
                UserId = currentUserId // Ödemeye kullanıcı ID'sini ekle
            });
        }

        sale.IsCompleted = true;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Satış parçalı ödeme ile tamamlandı.",
            totalAmount = sale.TotalAmount
        });
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