using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class SaleController : ControllerBase
{
    private readonly MyPosDbContext _context; // DbContext'iniz
    private readonly SaleValidator _validator; // DTO'ları doğrulamak için

    public SaleController(MyPosDbContext context)
    {
        _context = context;
        _validator = new SaleValidator();
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequestDto request)
    {
        // 1. Gelen veriyi doğrula
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors });
        }

        // 2. İş mantığı
        // Satış işlemini başlatmak için yeni bir Sale nesnesi oluştur.
        var sale = new Sale
        {
            CustomerId = request.CustomerId,
            SaleDate = DateTime.Now,
            IsCompleted = false,
            // TotalAmount ve PaymentType finalize edilene kadar 0 veya null kalır.
            TotalAmount = 0,
            PaymentType = null
        };
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        // Her bir satış kalemi için SaleItem oluştur.
        foreach (var itemDto in request.SaleItems)
        {
            // Ürün bilgilerini stoktan çek
            var product = await _context.Products.FindAsync(itemDto.ProductId);
            if (product == null)
            {
                // Ürün bulunamazsa hata dön
                return NotFound($"Ürün ID'si {itemDto.ProductId} bulunamadı.");
            }

            // Stok kontrolü yap (Satışın finalize edilmesi aşamasına da bırakılabilir)
            if (product.Stock < itemDto.Quantity)
            {
                return BadRequest($"Ürün '{product.Name}' için yeterli stok yok. Mevcut stok: {product.Stock}.");
            }

            var saleItem = new SaleItem
            {
                SaleId = sale.SaleId,
                ProductId = itemDto.ProductId,
                ProductName = product.Name,
                Quantity = itemDto.Quantity,
                UnitPrice = product.SalePrice, // Örnek olarak product.UnitPrice kullanıldı
                TotalPrice = itemDto.Quantity * product.SalePrice,
                Discount = 0
            };
            sale.TotalAmount += saleItem.TotalPrice;
            _context.SaleItems.Add(saleItem);
        }

        await _context.SaveChangesAsync();

        // Front-end'e oluşturulan satışın ID'sini ve detaylarını dön.
        var saleDetailsDto = new SaleDetailsDto
        {
            SaleId = sale.SaleId,
            CustomerId = sale.CustomerId,
            TotalAmount = sale.TotalAmount,
            SaleDate = sale.SaleDate,
            IsCompleted = sale.IsCompleted,
            SaleItems = sale.SaleItems.Select(si => new SaleItemDetailsDto
            {
                SaleItemId = si.SaleItemId,
                ProductId = si.ProductId,
                ProductName = si.ProductName,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TotalPrice = si.TotalPrice,
                Discount = si.Discount
            }).ToList()
        };

        return CreatedAtAction(nameof(GetSaleById), new { saleId = sale.SaleId }, saleDetailsDto);
    }

    [HttpPost("{saleId}/finalize")]
    public async Task<IActionResult> FinalizeSale(int saleId, [FromBody] FinalizeSaleRequestDto request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Satışı bul
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);

            if (sale == null)
            {
                return NotFound($"Satış ID'si {saleId} bulunamadı.");
            }

            // Satış zaten tamamlanmış mı diye kontrol et
            if (sale.IsCompleted)
            {
                return BadRequest("Bu satış zaten tamamlanmış.");
            }

            // 1. Ödeme işlemini yap (bu kısım gerçek bir API çağrısı olabilir)
            sale.PaymentType = request.PaymentType;

            // 2. Stokları güncelle ve hareketleri kaydet
            foreach (var saleItem in sale.SaleItems)
            {
                var product = await _context.Products.FindAsync(saleItem.ProductId);
                if (product != null)
                {
                    // Stoktan düşüş
                    product.Stock -= saleItem.Quantity;

                    // Stok hareketini kaydet
                    _context.StockTransaction.Add(new StockTransaction
                    {
                        ProductId = product.Id,
                        QuantityChange = -saleItem.Quantity, // Satış olduğu için negatif değer
                        TransactionType = "OUT",
                        Reason = $"Sale:{saleId}",
                        Date = DateTime.Now,
                        BalanceAfter = product.Stock
                    });
                }
            }

            // 3. Satışı tamamlanmış olarak işaretle
            sale.IsCompleted = true;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Satış başarıyla tamamlandı.", saleId = sale.SaleId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Satış tamamlanırken hata oluştu: " + ex.Message);
        }
    }

    [HttpGet("{saleId}")]
    public async Task<IActionResult> GetSaleById(int saleId)
    {
        var sale = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.SaleId == saleId);

        if (sale == null)
        {
            return NotFound();
        }

        var saleDetailsDto = new SaleDetailsDto
        {
            SaleId = sale.SaleId,
            CustomerId = sale.CustomerId,
            CustomerName = sale.Customer?.CustomerName,
            TotalAmount = sale.TotalAmount,
            PaymentType = sale.PaymentType,
            SaleDate = sale.SaleDate,
            IsCompleted = sale.IsCompleted,
            SaleItems = sale.SaleItems.Select(si => new SaleItemDetailsDto
            {
                SaleItemId = si.SaleItemId,
                ProductId = si.ProductId,
                ProductName = si.ProductName,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TotalPrice = si.TotalPrice,
                Discount = si.Discount
            }).ToList()
        };

        return Ok(saleDetailsDto);
    }
}

// FinalizeSale için yeni bir DTO
public class FinalizeSaleRequestDto
{
    [Required]
    public string PaymentType { get; set; }
}