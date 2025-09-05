using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class PurchaseInvoicesController : ControllerBase
    {
        private readonly MyPosDbContext _context;
        private readonly IValidator<CreatePurchaseInvoiceDto> _createValidator;
        private readonly IValidator<UpdatePurchaseInvoiceDto> _updateValidator;

        public PurchaseInvoicesController(MyPosDbContext context,
                                          IValidator<CreatePurchaseInvoiceDto> createValidator,
                                          IValidator<UpdatePurchaseInvoiceDto> updateValidator)
        {
            _context = context;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseInvoice([FromBody] CreatePurchaseInvoiceDto createDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var paymentType = await _context.PaymentTypes.FindAsync(createDto.PaymentTypeId);
            if (paymentType == null)
                return BadRequest($"Ödeme tipi bulunamadı: {createDto.PaymentTypeId}");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newInvoice = new PurchaseInvoice
                {
                    InvoiceNumber = createDto.InvoiceNumber,
                    InvoiceDate = createDto.InvoiceDate,
                    CompanyId = createDto.CompanyId,
                    PaymentTypeId = createDto.PaymentTypeId,
                    DoesNotAffectProfit = createDto.DoesNotAffectProfit,
                    TotalAmount = 0,
                    TotalDiscount = 0,
                    TotalTaxAmount = 0,
                    GrandTotal = 0
                };

                _context.PurchaseInvoices.Add(newInvoice);
                await _context.SaveChangesAsync();

                foreach (var itemDto in createDto.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemDto.ProductId);
                    if (product == null)
                        return BadRequest($"Ürün bulunamadı: {itemDto.ProductId}");

                    // --- BURADA KURALIMIZ DEVREYE GİRİYOR ---
                    decimal unitPriceToUse = itemDto.UnitPrice;

                    if (unitPriceToUse <= 0) // Eğer gönderilen fiyat 0 veya boş ise
                    {
                        unitPriceToUse = product.PurchasePrice;
                    }
                    else // Eğer dolu gelirse
                    {
                        product.PurchasePrice = unitPriceToUse; // Ürün alış fiyatını güncelle
                    }
                    // --------------------------------------

                    var itemTotalPrice = itemDto.Quantity * unitPriceToUse;
                    var discountAmount1 = itemTotalPrice * (itemDto.DiscountRate1 ?? 0) / 100;
                    var priceAfterFirstDiscount = itemTotalPrice - discountAmount1;
                    var discountAmount2 = priceAfterFirstDiscount * (itemDto.DiscountRate2 ?? 0) / 100;
                    var totalAfterDiscount = priceAfterFirstDiscount - discountAmount2;
                    var itemDiscount = discountAmount1 + discountAmount2;
                    var itemTaxAmount = totalAfterDiscount * itemDto.TaxRate / 100;

                    var newItem = new PurchaseInvoiceItem
                    {
                        PurchaseInvoiceId = newInvoice.Id,
                        ProductId = product.Id,
                        Barcode = product.Barcode,
                        ProductName = product.Name,
                        Quantity = itemDto.Quantity,
                        UnitPrice = unitPriceToUse,
                        TotalPrice = itemTotalPrice,
                        TaxRate = itemDto.TaxRate,
                        TaxAmount = itemTaxAmount,
                        DiscountRate1 = itemDto.DiscountRate1,
                        DiscountRate2 = itemDto.DiscountRate2,
                        TotalDiscountAmount = itemDiscount
                    };

                    _context.PurchaseInvoiceItems.Add(newItem);

                    // stok güncelle
                    product.Stock += itemDto.Quantity;

                    _context.StockTransaction.Add(new StockTransaction
                    {
                        ProductId = product.Id,
                        QuantityChange = itemDto.Quantity,
                        TransactionType = "IN",
                        Reason = $"PurchaseInvoice:{newInvoice.Id}",
                        Date = DateTime.Now,
                        BalanceAfter = product.Stock
                    });

                    newInvoice.TotalAmount += itemTotalPrice;
                    newInvoice.TotalDiscount += itemDiscount;
                    newInvoice.TotalTaxAmount += itemTaxAmount;
                    newInvoice.GrandTotal += totalAfterDiscount + itemTaxAmount;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Invoice created successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Bir hata oluştu: " + ex.Message);
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePurchaseInvoice(int id, [FromBody] UpdatePurchaseInvoiceDto updateDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            // Check if the payment type exists
            var paymentType = await _context.PaymentTypes.FindAsync(updateDto.PaymentTypeId);
            if (paymentType == null)
                return BadRequest($"Ödeme tipi bulunamadı: {updateDto.PaymentTypeId}");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invoice = await _context.PurchaseInvoices
                    .Include(i => i.PurchaseInvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                    return NotFound($"Fatura bulunamadı: {id}");

                // Eski stokları geri al
                foreach (var oldItem in invoice.PurchaseInvoiceItems)
                {
                    var product = await _context.Products.FindAsync(oldItem.ProductId);
                    if (product != null)
                    {
                        product.Stock -= oldItem.Quantity;

                        _context.StockTransaction.Add(new StockTransaction
                        {
                            ProductId = product.Id,
                            QuantityChange = -oldItem.Quantity,
                            TransactionType = "OUT",
                            Reason = $"PurchaseInvoiceUpdate-Revert:{id}",
                            Date = DateTime.Now,
                            BalanceAfter = product.Stock
                        });
                    }
                }

                _context.PurchaseInvoiceItems.RemoveRange(invoice.PurchaseInvoiceItems);

                invoice.InvoiceNumber = updateDto.InvoiceNumber;
                invoice.InvoiceDate = updateDto.InvoiceDate;
                invoice.CompanyId = updateDto.CompanyId;
                invoice.PaymentTypeId = updateDto.PaymentTypeId;
                invoice.DoesNotAffectProfit = updateDto.DoesNotAffectProfit;
                invoice.TotalAmount = 0;
                invoice.TotalDiscount = 0;
                invoice.TotalTaxAmount = 0;
                invoice.GrandTotal = 0;

                foreach (var itemDto in updateDto.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemDto.ProductId);
                    if (product == null)
                        return BadRequest($"Ürün bulunamadı: {itemDto.ProductId}");

                    var itemTotalPrice = itemDto.Quantity * itemDto.UnitPrice;
                    var discountAmount1 = itemTotalPrice * (itemDto.DiscountRate1 ?? 0) / 100;
                    var priceAfterFirstDiscount = itemTotalPrice - discountAmount1;
                    var discountAmount2 = priceAfterFirstDiscount * (itemDto.DiscountRate2 ?? 0) / 100;
                    var totalAfterDiscount = priceAfterFirstDiscount - discountAmount2;
                    var itemDiscount = discountAmount1 + discountAmount2;
                    var itemTaxAmount = totalAfterDiscount * itemDto.TaxRate / 100;

                    var newItem = new PurchaseInvoiceItem
                    {
                        PurchaseInvoiceId = invoice.Id,
                        ProductId = product.Id,
                        Barcode = product.Barcode,
                        ProductName = product.Name,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        TotalPrice = itemTotalPrice,
                        TaxRate = itemDto.TaxRate,
                        TaxAmount = itemTaxAmount,
                        DiscountRate1 = itemDto.DiscountRate1,
                        DiscountRate2 = itemDto.DiscountRate2,
                        TotalDiscountAmount = itemDiscount
                    };

                    invoice.PurchaseInvoiceItems.Add(newItem);

                    product.Stock += itemDto.Quantity;

                    _context.StockTransaction.Add(new StockTransaction
                    {
                        ProductId = product.Id,
                        QuantityChange = itemDto.Quantity,
                        TransactionType = "IN",
                        Reason = $"PurchaseInvoiceUpdate-New:{id}",
                        Date = DateTime.Now,
                        BalanceAfter = product.Stock
                    });

                    invoice.TotalAmount += itemTotalPrice;
                    invoice.TotalDiscount += itemDiscount;
                    invoice.TotalTaxAmount += itemTaxAmount;
                    invoice.GrandTotal += totalAfterDiscount + itemTaxAmount;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Fatura güncellenirken hata oluştu: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseInvoice(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invoice = await _context.PurchaseInvoices
                    .Include(i => i.PurchaseInvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                    return NotFound($"Fatura bulunamadı: {id}");

                foreach (var item in invoice.PurchaseInvoiceItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock -= item.Quantity;

                        _context.StockTransaction.Add(new StockTransaction
                        {
                            ProductId = product.Id,
                            QuantityChange = -item.Quantity,
                            TransactionType = "OUT",
                            Reason = $"PurchaseInvoiceDelete:{id}",
                            Date = DateTime.Now,
                            BalanceAfter = product.Stock
                        });
                    }
                }

                _context.PurchaseInvoiceItems.RemoveRange(invoice.PurchaseInvoiceItems);
                _context.PurchaseInvoices.Remove(invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Fatura silinirken hata oluştu: " + ex.Message);
            }
        }
        [HttpGet("GetCompanyInvoices/{companyId}")]
        public async Task<IActionResult> GetCompanyInvoices(int companyId, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.PurchaseInvoices
                .Include(x => x.Company)
                .Where(x => x.CompanyId == companyId);

            if (startDate.HasValue)
                query = query.Where(x => x.InvoiceDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.InvoiceDate <= endDate.Value);

            var invoices = await query
    .Select(x => new PurchaseInvoiceListDto
    {
        Id = x.Id,
        InvoiceNumber = x.InvoiceNumber,
        InvoiceDate = x.InvoiceDate,
        CompanyName = x.Company.Name,
        GrandTotal = x.GrandTotal,
        PaymentTypeName = x.PaymentType != null ? x.PaymentType.Name : string.Empty,
        PaymentTypeCashRegister = x.PaymentType != null ? x.PaymentType.CashRegisterType.ToString() : string.Empty
    })
    .ToListAsync();

            var totalAmount = invoices.Sum(i => i.GrandTotal);

            // Kasa türüne göre gruplama
            var totalsByCashRegister = invoices
                .GroupBy(i => i.PaymentTypeCashRegister)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.GrandTotal));

            var response = new
            {
                TotalAmount = totalAmount,
                TotalsByCashRegister = totalsByCashRegister, // {"Nakit": 1500, "Pos": 2500, "Açık Hesap": 500}
                Invoices = invoices
            };

            return Ok(response);
        }


        [HttpGet("all")]
        public async Task<ActionResult<List<PurchaseInvoiceDetailsDto>>> GetAllPurchaseInvoices()
        {
            var invoices = await _context.PurchaseInvoices
                .Include(i => i.Company)
                .Include(i => i.PurchaseInvoiceItems)
                .Include(i => i.PaymentType) // Added to include payment type data
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            var dtoList = invoices.Select(invoice => new PurchaseInvoiceDetailsDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                CompanyName = invoice.Company != null ? invoice.Company.Name : string.Empty,
                TotalAmount = invoice.TotalAmount,
                GrandTotal = invoice.GrandTotal,
                PaymentTypeName = invoice.PaymentType != null ? invoice.PaymentType.Name : string.Empty,
                DoesNotAffectProfit = invoice.DoesNotAffectProfit,
                Items = invoice.PurchaseInvoiceItems.Select(item => new PurchaseInvoiceItemDetailsDto
                {
                    ProductName = item.ProductName,
                    Barcode = item.Barcode,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    TaxRate = item.TaxRate,
                    DiscountRate1 = item.DiscountRate1,
                    DiscountRate2 = item.DiscountRate2
                }).ToList()
            }).ToList();

            return Ok(dtoList);
        }
    }
}