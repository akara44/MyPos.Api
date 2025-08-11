using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Application.Validators;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;
using System.Transactions; // İşlem yönetimi için gerekli

namespace MyPos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseInvoicesController : ControllerBase
    {
        private readonly MyPosDbContext _context;
        private readonly IValidator<CreatePurchaseInvoiceDto> _validator;

        public PurchaseInvoicesController(MyPosDbContext context, IValidator<CreatePurchaseInvoiceDto> validator)
        {
            _context = context;
            _validator = validator;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseInvoice([FromBody] CreatePurchaseInvoiceDto createDto)
        {
            var validationResult = await _validator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Transaction kullanarak atomik bir işlem sağlamak
            // Eğer bir hata oluşursa, veritabanına yapılan tüm değişiklikler geri alınır.
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. DTO'dan PurchaseInvoice entity'sine dönüşüm
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

                    // 2. Fatura kalemlerini oluşturma ve hesaplamalar
                    foreach (var itemDto in createDto.Items)
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

                        // 3. Stok güncellemesi
                        product.Stock += itemDto.Quantity;

                        // Fatura genel toplamlarını güncelle
                        newInvoice.TotalAmount += itemTotalPrice;
                        newInvoice.TotalDiscount += itemDiscount;
                        newInvoice.TotalTaxAmount += itemTaxAmount;
                        newInvoice.GrandTotal += totalAfterDiscount + itemTaxAmount;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // İstemciye geri dönüş DTO'su oluşturma
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
                    // Loglama yapılabilir
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
    }
}