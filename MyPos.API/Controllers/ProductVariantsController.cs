using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductVariantsController : ControllerBase
    {
        private readonly MyPosDbContext _context;
        private readonly IValidator<CreateProductVariantDto> _createProductVariantValidator;
        private readonly IValidator<UpdateProductVariantDto> _updateProductVariantValidator;

        public ProductVariantsController(
            MyPosDbContext context,
            IValidator<CreateProductVariantDto> createProductVariantValidator,
            IValidator<UpdateProductVariantDto> updateProductVariantValidator)
        {
            _context = context;
            _createProductVariantValidator = createProductVariantValidator;
            _updateProductVariantValidator = updateProductVariantValidator;
        }

        // Tüm ürün varyantlarını listele (isteğe bağlı olarak ürün ID'ye göre filtrele)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? productId = null)
        {
            var query = _context.ProductVariants
                .Include(pv => pv.Product)
                .Include(pv => pv.ProductVariantValues)
                    .ThenInclude(pvv => pvv.VariantValue)
                        .ThenInclude(vv => vv.VariantType)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(pv => pv.ProductId == productId.Value);
            }

            var productVariants = await query
                .Select(pv => new ProductVariantResponseDto
                {
                    Id = pv.Id,
                    ProductId = pv.ProductId,
                    ProductName = pv.Product != null ? pv.Product.Name : string.Empty,
                    Barcode = pv.Barcode,
                    Name = pv.Name,
                    Stock = pv.Stock,
                    CriticalStockLevel = pv.CriticalStockLevel,
                    SalePrice = pv.SalePrice,
                    PurchasePrice = pv.PurchasePrice,
                    ProfitRate = pv.ProfitRate,
                    TaxRate = pv.TaxRate,
                    SalePageList = pv.SalePageList,
                    ImageUrl = pv.ImageUrl,
                    VariantValueIds = pv.ProductVariantValues.Select(pvv => pvv.VariantValueId).ToList(),
                    VariantDetails = pv.ProductVariantValues
                        .Select(pvv => new VariantDetailDto
                        {
                            VariantTypeId = pvv.VariantValue.VariantTypeId,
                            VariantTypeName = pvv.VariantValue.VariantType != null ? pvv.VariantValue.VariantType.Name : string.Empty,
                            VariantValueId = pvv.VariantValue.Id,
                            VariantValueName = pvv.VariantValue.Value
                        }).ToList()
                })
                .ToListAsync();

            return Ok(productVariants);
        }

        // ID'ye göre ürün varyantı getir
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var productVariant = await _context.ProductVariants
                .Include(pv => pv.Product)
                .Include(pv => pv.ProductVariantValues)
                    .ThenInclude(pvv => pvv.VariantValue)
                        .ThenInclude(vv => vv.VariantType)
                .FirstOrDefaultAsync(pv => pv.Id == id);

            if (productVariant == null)
            {
                return NotFound();
            }

            var response = new ProductVariantResponseDto
            {
                Id = productVariant.Id,
                ProductId = productVariant.ProductId,
                ProductName = productVariant.Product != null ? productVariant.Product.Name : string.Empty,
                Barcode = productVariant.Barcode,
                Name = productVariant.Name,
                Stock = productVariant.Stock,
                CriticalStockLevel = productVariant.CriticalStockLevel,
                SalePrice = productVariant.SalePrice,
                PurchasePrice = productVariant.PurchasePrice,
                ProfitRate = productVariant.ProfitRate,
                TaxRate = productVariant.TaxRate,
                SalePageList = productVariant.SalePageList,
                ImageUrl = productVariant.ImageUrl,
                VariantValueIds = productVariant.ProductVariantValues.Select(pvv => pvv.VariantValueId).ToList(),
                VariantDetails = productVariant.ProductVariantValues
                    .Select(pvv => new VariantDetailDto
                    {
                        VariantTypeId = pvv.VariantValue.VariantTypeId,
                        VariantTypeName = pvv.VariantValue.VariantType != null ? pvv.VariantValue.VariantType.Name : string.Empty,
                        VariantValueId = pvv.VariantValue.Id,
                        VariantValueName = pvv.VariantValue.Value
                    }).ToList()
            };
            return Ok(response);
        }

        // Yeni ürün varyantı oluştur
        [HttpPost]
        public async Task<ActionResult<ProductVariantResponseDto>> Create([FromBody] CreateProductVariantDto dto)
        {
            var validationResult = await _createProductVariantValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Ana ürünün varlığını kontrol edin
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return BadRequest("Belirtilen ana ürün bulunamadı.");
            }

            // Varyant değerlerinin varlığını ve aynı VaryantType'tan sadece bir tane seçildiğini kontrol edin
            var variantValues = await _context.VariantValues
                .Include(vv => vv.VariantType)
                .Where(vv => dto.VariantValueIds.Contains(vv.Id))
                .ToListAsync();

            if (variantValues.Count != dto.VariantValueIds.Count)
            {
                return BadRequest("Bazı varyant değerleri bulunamadı.");
            }

            // Aynı varyant tipinden birden fazla değer seçilmediğini kontrol edin
            var variantTypeCounts = variantValues
                .GroupBy(vv => vv.VariantTypeId)
                .Select(g => new { VariantTypeId = g.Key, Count = g.Count() })
                .ToList();

            if (variantTypeCounts.Any(x => x.Count > 1))
            {
                return BadRequest("Aynı varyant tipinden birden fazla değer seçilemez.");
            }

            // Eğer isterseniz, aynı varyant kombinasyonunun zaten var olup olmadığını kontrol edin
            // Bu daha karmaşık bir sorgu gerektirir.

            var productVariant = new ProductVariant
            {
                ProductId = dto.ProductId,
                Barcode = dto.Barcode,
                Name = dto.Name,
                Stock = dto.Stock,
                CriticalStockLevel = dto.CriticalStockLevel,
                SalePrice = dto.SalePrice,
                PurchasePrice = dto.PurchasePrice,
                TaxRate = dto.TaxRate,
                SalePageList = dto.SalePageList,
            };

            _context.ProductVariants.Add(productVariant);
            await _context.SaveChangesAsync(); // ProductVariant'ın ID'si oluşması için kaydet

            // ProductVariantValue ilişkilerini oluştur
            foreach (var variantValue in variantValues)
            {
                productVariant.ProductVariantValues.Add(new ProductVariantValue
                {
                    ProductVariantId = productVariant.Id,
                    VariantValueId = variantValue.Id
                });
            }
            await _context.SaveChangesAsync(); // İlişkileri kaydet

            var response = new ProductVariantResponseDto
            {
                Id = productVariant.Id,
                ProductId = productVariant.ProductId,
                ProductName = product.Name,
                Barcode = productVariant.Barcode,
                Name = productVariant.Name,
                Stock = productVariant.Stock,
                CriticalStockLevel = productVariant.CriticalStockLevel,
                SalePrice = productVariant.SalePrice,
                PurchasePrice = productVariant.PurchasePrice,
                ProfitRate = productVariant.ProfitRate,
                TaxRate = productVariant.TaxRate,
                SalePageList = productVariant.SalePageList,
                ImageUrl = productVariant.ImageUrl,
                VariantValueIds = dto.VariantValueIds,
                VariantDetails = variantValues
                    .Select(vv => new VariantDetailDto
                    {
                        VariantTypeId = vv.VariantTypeId,
                        VariantTypeName = vv.VariantType.Name,
                        VariantValueId = vv.Id,
                        VariantValueName = vv.Value
                    }).ToList()
            };

            return CreatedAtAction(nameof(GetById), new { id = productVariant.Id }, response);
        }

        // Ürün varyantı güncelle
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductVariantDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest("ID'ler eşleşmiyor.");
            }

            var validationResult = await _updateProductVariantValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var productVariant = await _context.ProductVariants
                .Include(pv => pv.ProductVariantValues)
                .ThenInclude(pvv => pvv.VariantValue)
                .ThenInclude(vv => vv.VariantType)
                .FirstOrDefaultAsync(pv => pv.Id == id);

            if (productVariant == null)
            {
                return NotFound();
            }

            // Ana ürünün varlığını kontrol edin (eğer değişiyorsa)
            if (productVariant.ProductId != dto.ProductId)
            {
                var newProduct = await _context.Products.FindAsync(dto.ProductId);
                if (newProduct == null)
                {
                    return BadRequest("Belirtilen yeni ana ürün bulunamadı.");
                }
                productVariant.Product = newProduct; // Navigasyon özelliğini güncelleyin
            }

            // Varyant değerlerinin varlığını ve aynı VaryantType'tan sadece bir tane seçildiğini kontrol edin
            var newVariantValues = await _context.VariantValues
                .Include(vv => vv.VariantType)
                .Where(vv => dto.VariantValueIds.Contains(vv.Id))
                .ToListAsync();

            if (newVariantValues.Count != dto.VariantValueIds.Count)
            {
                return BadRequest("Bazı varyant değerleri bulunamadı.");
            }

            var newVariantTypeCounts = newVariantValues
                .GroupBy(vv => vv.VariantTypeId)
                .Select(g => new { VariantTypeId = g.Key, Count = g.Count() })
                .ToList();

            if (newVariantTypeCounts.Any(x => x.Count > 1))
            {
                return BadRequest("Aynı varyant tipinden birden fazla değer seçilemez.");
            }

            // Mevcut ilişkileri temizle ve yenilerini ekle
            _context.ProductVariantValues.RemoveRange(productVariant.ProductVariantValues);
            foreach (var variantValue in newVariantValues)
            {
                productVariant.ProductVariantValues.Add(new ProductVariantValue
                {
                    ProductVariantId = productVariant.Id,
                    VariantValueId = variantValue.Id
                });
            }

            productVariant.ProductId = dto.ProductId;
            productVariant.Barcode = dto.Barcode;
            productVariant.Name = dto.Name;
            productVariant.Stock = dto.Stock;
            productVariant.CriticalStockLevel = dto.CriticalStockLevel;
            productVariant.SalePrice = dto.SalePrice;
            productVariant.PurchasePrice = dto.PurchasePrice;
            productVariant.TaxRate = dto.TaxRate;
            productVariant.SalePageList = dto.SalePageList;

            await _context.SaveChangesAsync();

            // Güncellenmiş yanıtı döndür
            var updatedProduct = await _context.Products.FindAsync(productVariant.ProductId);
            var response = new ProductVariantResponseDto
            {
                Id = productVariant.Id,
                ProductId = productVariant.ProductId,
                ProductName = updatedProduct != null ? updatedProduct.Name : string.Empty,
                Barcode = productVariant.Barcode,
                Name = productVariant.Name,
                Stock = productVariant.Stock,
                CriticalStockLevel = productVariant.CriticalStockLevel,
                SalePrice = productVariant.SalePrice,
                PurchasePrice = productVariant.PurchasePrice,
                ProfitRate = productVariant.ProfitRate,
                TaxRate = productVariant.TaxRate,
                SalePageList = productVariant.SalePageList,
                ImageUrl = productVariant.ImageUrl,
                VariantValueIds = dto.VariantValueIds,
                VariantDetails = newVariantValues
                    .Select(vv => new VariantDetailDto
                    {
                        VariantTypeId = vv.VariantTypeId,
                        VariantTypeName = vv.VariantType.Name,
                        VariantValueId = vv.Id,
                        VariantValueName = vv.Value
                    }).ToList()
            };

            return Ok(response);
        }

        // Ürün varyantı sil
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var productVariant = await _context.ProductVariants.FindAsync(id);
            if (productVariant == null)
            {
                return NotFound();
            }

            _context.ProductVariants.Remove(productVariant);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Varyanta özel resim yükleme
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            var productVariant = await _context.ProductVariants.FindAsync(id);
            if (productVariant == null)
                return NotFound("Ürün varyantı bulunamadı.");

            if (file == null || file.Length == 0)
                return BadRequest("Geçerli bir dosya seçilmedi.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                return BadRequest("Sadece .jpg, .jpeg veya .png dosyaları yüklenebilir.");

            var fileName = $"{Guid.NewGuid()}{extension}";
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "variants"); // Varyant resimleri için ayrı bir klasör
            var fullPath = Path.Combine(folderPath, fileName);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Eski dosyayı sil
            if (!string.IsNullOrEmpty(productVariant.ImageUrl))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", productVariant.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            productVariant.ImageUrl = $"/images/variants/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { imageUrl = productVariant.ImageUrl });
        }
    }
}