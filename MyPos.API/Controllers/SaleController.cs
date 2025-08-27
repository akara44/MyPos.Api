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
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors });

        // 🔥 Tek seferde bütün Product'ları çek
        var productIds = request.SaleItems.Select(x => x.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var sale = new Sale
        {
            CustomerId = request.CustomerId,
            SaleDate = DateTime.Now,
            IsCompleted = false,
            TotalAmount = 0,
            PaymentType = null,
            SaleItems = new List<SaleItem>()
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
                Discount = 0
            };

            sale.TotalAmount += saleItem.TotalPrice;
            sale.SaleItems.Add(saleItem);
        }

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        // 🔥 Customer lookup sadece gerekirse yapılacak
        string? customerName = null;
        if (sale.CustomerId.HasValue)
            customerName = await _context.Customers
                .Where(c => c.Id == sale.CustomerId.Value)
                .Select(c => c.CustomerName)
                .FirstOrDefaultAsync();

        var saleDetailsDto = new SaleDetailsDto
        {
            SaleId = sale.SaleId,
            CustomerId = sale.CustomerId,
            CustomerName = customerName,
            TotalAmount = sale.TotalAmount,
            SaleDate = sale.SaleDate,
            IsCompleted = sale.IsCompleted,
            PaymentType = sale.PaymentType,
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
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);

            if (sale == null)
                return NotFound($"Satış ID'si {saleId} bulunamadı.");

            if (sale.IsCompleted)
                return BadRequest("Bu satış zaten tamamlanmış.");

            var paymentType = await _context.PaymentTypes.FindAsync(request.PaymentTypeId);
            if (paymentType == null)
                return BadRequest("Seçilen ödeme tipi bulunamadı.");

            sale.PaymentType = paymentType.Name;

            // 🔥 Tek query ile tüm ürünleri al
            var productIds = sale.SaleItems.Select(si => si.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var saleItem in sale.SaleItems)
            {
                if (!products.TryGetValue(saleItem.ProductId, out var product))
                    continue;

                product.Stock -= saleItem.Quantity;

                _context.StockTransaction.Add(new StockTransaction
                {
                    ProductId = product.Id,
                    QuantityChange = -saleItem.Quantity,
                    TransactionType = "OUT",
                    Reason = $"Sale:{saleId}",
                    Date = DateTime.Now,
                    BalanceAfter = product.Stock
                });
            }

            sale.IsCompleted = true;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Satış başarıyla tamamlandı.",
                saleId = sale.SaleId,
                paymentType = sale.PaymentType
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
        var sale = await _context.Sales.FindAsync(saleId);
        if (sale == null) return NotFound("Satış bulunamadı.");
        if (sale.IsCompleted) return BadRequest("Satış zaten tamamlanmış.");

        // parçalı toplam kontrolü
        var totalSplit = request.SplitPayments.Sum(x => x.Amount);
        if (totalSplit != sale.TotalAmount)
            return BadRequest($"Parçalı ödemelerin toplamı ({totalSplit}) satış tutarına ({sale.TotalAmount}) eşit değil.");

        foreach (var sp in request.SplitPayments)
        {
            string paymentTypeName;

            // Default yöntemler
            if (sp.PaymentTypeName == "Nakit" || sp.PaymentTypeName == "POS")
            {
                paymentTypeName = sp.PaymentTypeName;
            }
            else
            {
                // DB'den kontrol
                if (sp.PaymentTypeId == null)
                    return BadRequest("DB ödeme tipi için PaymentTypeId boş olamaz.");

                var paymentType = await _context.PaymentTypes.FindAsync(sp.PaymentTypeId.Value);
                if (paymentType == null) return BadRequest($"Geçersiz ödeme tipi: {sp.PaymentTypeId}");
                paymentTypeName = paymentType.Name;
            }

            _context.Payments.Add(new Payment
            {
                SaleId = sale.SaleId,
                CustomerId = sale.CustomerId ?? 0,
                Amount = sp.Amount,
                PaymentDate = DateTime.Now,
                PaymentTypeName = paymentTypeName
            });
        }

        sale.IsCompleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Satış parçalı ödeme ile tamamlandı." });
    }


    [HttpGet("{saleId}")]
    public async Task<IActionResult> GetSaleById(int saleId)
    {
        var sale = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.SaleId == saleId);

        if (sale == null)
            return NotFound();

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
    [HttpGet("all")]
    public async Task<IActionResult> GetAllSales()
    {
        var sales = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .ToListAsync();

        var salesDto = sales.Select(s => new SaleDetailsDto
        {
            SaleId = s.SaleId,
            CustomerId = s.CustomerId,
            CustomerName = s.Customer?.CustomerName,
            TotalAmount = s.TotalAmount,
            PaymentType = s.PaymentType,
            SaleDate = s.SaleDate,
            IsCompleted = s.IsCompleted,
            SaleItems = s.SaleItems.Select(si => new SaleItemDetailsDto
            {
                SaleItemId = si.SaleItemId,
                ProductId = si.ProductId,
                ProductName = si.ProductName,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TotalPrice = si.TotalPrice,
                Discount = si.Discount
            }).ToList()
        }).ToList();

        return Ok(salesDto);
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
    public int? PaymentTypeId { get; set; }     // sadece DB’den gelenler için kullanılır
    public decimal Amount { get; set; }
}
