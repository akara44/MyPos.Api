using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Application.Validators;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Transactions; // TransactionScope için gerekli

namespace MyPos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            {
                return BadRequest(validationResult.Errors);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var newInvoice = new PurchaseInvoice
                    {
                        InvoiceNumber = createDto.InvoiceNumber,
                        InvoiceDate = createDto.InvoiceDate,
                        CompanyId = createDto.CompanyId,
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
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Ürün bulunamadı: ProductId = {itemDto.ProductId}");
                        }

                        // 1️⃣ Toplam fiyat (KDV ve indirim hariç)
                        var itemTotalPrice = itemDto.Quantity * itemDto.UnitPrice;

                        // 2️⃣ İndirimleri sırayla uygula
                        var discountAmount1 = itemTotalPrice * (itemDto.DiscountRate1 ?? 0) / 100;
                        var priceAfterFirstDiscount = itemTotalPrice - discountAmount1;

                        var discountAmount2 = priceAfterFirstDiscount * (itemDto.DiscountRate2 ?? 0) / 100;
                        var totalAfterDiscount = priceAfterFirstDiscount - discountAmount2;

                        // 3️⃣ Toplam indirim tutarı
                        var itemDiscount = discountAmount1 + discountAmount2;

                        // 4️⃣ KDV'yi indirimli fiyattan hesapla
                        var itemTaxAmount = totalAfterDiscount * itemDto.TaxRate / 100;

                        // 5️⃣ Yeni satır ekle
                        var newItem = new PurchaseInvoiceItem
                        {
                            PurchaseInvoiceId = newInvoice.Id,
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

                        _context.PurchaseInvoiceItems.Add(newItem);

                        // Stok artır
                        product.Stock += itemDto.Quantity;

                        // Stok hareketini kaydet
                        var stockTransaction = new StockTransaction
                        {
                            ProductId = product.Id,
                            QuantityChange = itemDto.Quantity,
                            TransactionType = "IN", // Alış olduğu için giriş
                            Reason = "PurchaseInvoice",
                            Date = DateTime.Now
                        };
                        _context.StockTransaction.Add(stockTransaction);


                        // Fatura toplamlarını güncelle
                        newInvoice.TotalAmount += itemTotalPrice;
                        newInvoice.TotalDiscount += itemDiscount;
                        newInvoice.TotalTaxAmount += itemTaxAmount;
                        newInvoice.GrandTotal += totalAfterDiscount + itemTaxAmount;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var resultDto = new PurchaseInvoiceDetailsDto
                    {
                        Id = newInvoice.Id,
                        InvoiceNumber = newInvoice.InvoiceNumber,
                        InvoiceDate = newInvoice.InvoiceDate,
                        CompanyName = (await _context.Company.FindAsync(newInvoice.CompanyId))?.Name ?? "Bilinmiyor",
                        TotalAmount = newInvoice.TotalAmount,
                        GrandTotal = newInvoice.GrandTotal,
                        Items = newInvoice.PurchaseInvoiceItems.Select(item => new PurchaseInvoiceItemDetailsDto
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
                    };

                    return CreatedAtAction(nameof(GetPurchaseInvoiceById), new { id = newInvoice.Id }, resultDto);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Bir hata oluştu: " + ex.Message);
                }
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPurchaseInvoiceById(int id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(i => i.Company)
                .Include(i => i.PurchaseInvoiceItems)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            var resultDto = new PurchaseInvoiceDetailsDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                CompanyName = invoice.Company?.Name ?? "Bilinmiyor",
                TotalAmount = invoice.TotalAmount,
                GrandTotal = invoice.GrandTotal,
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
            };

            return Ok(resultDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePurchaseInvoice(int id, [FromBody] UpdatePurchaseInvoiceDto updateDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var invoiceToUpdate = await _context.PurchaseInvoices
                        .Include(i => i.PurchaseInvoiceItems)
                        .FirstOrDefaultAsync(i => i.Id == id);

                    if (invoiceToUpdate == null)
                    {
                        return NotFound($"ID'si {id} olan fatura bulunamadı.");
                    }

                    // Güncelleme öncesi eski stokları geri al
                    foreach (var oldItem in invoiceToUpdate.PurchaseInvoiceItems)
                    {
                        var product = await _context.Products.FindAsync(oldItem.ProductId);
                        if (product != null)
                        {
                            product.Stock -= oldItem.Quantity;
                        }
                    }

                    // Eski kalemleri sil
                    _context.PurchaseInvoiceItems.RemoveRange(invoiceToUpdate.PurchaseInvoiceItems);

                    // Faturayı güncelle
                    invoiceToUpdate.InvoiceNumber = updateDto.InvoiceNumber;
                    invoiceToUpdate.InvoiceDate = updateDto.InvoiceDate;
                    invoiceToUpdate.CompanyId = updateDto.CompanyId;
                    invoiceToUpdate.TotalAmount = 0;
                    invoiceToUpdate.TotalDiscount = 0;
                    invoiceToUpdate.TotalTaxAmount = 0;
                    invoiceToUpdate.GrandTotal = 0;

                    // Yeni kalemleri oluştur ve stokları güncelle
                    foreach (var itemDto in updateDto.Items)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemDto.ProductId);
                        if (product == null)
                        {
                            return BadRequest($"Ürün bulunamadı: ProductId = {itemDto.ProductId}");
                        }

                        var itemTotalPrice = itemDto.Quantity * itemDto.UnitPrice;
                        var itemDiscount = (itemTotalPrice * (itemDto.DiscountRate1 ?? 0) / 100) + (itemTotalPrice * (itemDto.DiscountRate2 ?? 0) / 100);
                        var totalAfterDiscount = itemTotalPrice - itemDiscount;
                        var itemTaxAmount = totalAfterDiscount * itemDto.TaxRate / 100;

                        var newItem = new PurchaseInvoiceItem
                        {
                            PurchaseInvoiceId = invoiceToUpdate.Id,
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

                        invoiceToUpdate.PurchaseInvoiceItems.Add(newItem);
                        product.Stock += itemDto.Quantity;

                        invoiceToUpdate.TotalAmount += itemTotalPrice;
                        invoiceToUpdate.TotalDiscount += itemDiscount;
                        invoiceToUpdate.TotalTaxAmount += itemTaxAmount;
                        invoiceToUpdate.GrandTotal += totalAfterDiscount + itemTaxAmount;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return NoContent();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Fatura güncellenirken bir hata oluştu: " + ex.Message);
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseInvoice(int id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var invoiceToDelete = await _context.PurchaseInvoices
                        .Include(i => i.PurchaseInvoiceItems)
                        .FirstOrDefaultAsync(i => i.Id == id);

                    if (invoiceToDelete == null)
                    {
                        return NotFound($"ID'si {id} olan fatura bulunamadı.");
                    }

                    // Stokları geri al
                    foreach (var item in invoiceToDelete.PurchaseInvoiceItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.Stock -= item.Quantity;
                        }
                    }

                    // Fatura kalemlerini veritabanından silmek için RemoveRange kullanmak yerine
                    // ana varlıktan (fatura) alt varlıkları (kalemleri) silme talimatı verelim.
                    // Bu, bazı durumlarda EF'nin durumu daha iyi anlamasını sağlar.
                    _context.PurchaseInvoiceItems.RemoveRange(invoiceToDelete.PurchaseInvoiceItems);

                    // Faturanın kendisini sil
                    _context.Remove(invoiceToDelete);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return NoContent();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Lütfen bu logu kontrol et, asıl hata mesajı burada gizli olabilir!
                    Console.WriteLine(ex.InnerException?.Message);
                    return StatusCode(500, "Fatura silinirken bir hata oluştu: " + ex.Message);
                }
            }
        }
    }
}