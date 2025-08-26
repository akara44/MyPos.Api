using FluentValidation;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using MyPos.Infrastructure.Persistence;using MyPos.Application.Dtos;using MyPos.Domain.Entities;using MyPos.Application.Validators;namespace MyPos.Api.Controllers{    [Route("api/[controller]")]    [ApiController]    public class ProductsController : ControllerBase    {        private readonly MyPosDbContext _context;                private readonly IValidator<CreateProductDto> _createProductValidator;        private readonly IValidator<UpdateProductWithImageDto> _updateProductValidator;        public ProductsController(            MyPosDbContext context,            IValidator<CreateProductDto> createProductValidator,            IValidator<UpdateProductWithImageDto> updateProductValidator) // Update validator'ı enjekte edin        {            _context = context;            _createProductValidator = createProductValidator;            _updateProductValidator = updateProductValidator;        }        // Ürün Listeleme        [HttpGet]        public async Task<IActionResult> GetAll()        {            var products = await _context.Products                .Include(p => p.ProductGroup) // Adını almak için ProductGroup'u dahil edin                .Select(p => new ProductResponseDto // Tutarlı çıktı için ProductResponseDto kullanın                {                    Id = p.Id,                    Barcode = p.Barcode,                    Name = p.Name,                    Stock = p.Stock,                    SalePrice = p.SalePrice,                    PurchasePrice = p.PurchasePrice,                    ProfitRate = p.ProfitRate,                     TaxRate = p.TaxRate,                    ProductGroupId = p.ProductGroupId,                     ProductGroupName = p.ProductGroup.Name,                     ImageUrl = p.ImageUrl,                    CriticalStockLevel = p.CriticalStockLevel,                    SalePageList = p.SalePageList,                    Unit = p.Unit,                    OriginCountry = p.OriginCountry                })                .ToListAsync();            return Ok(products);        }

        // Ürün Ekleme
        // Ürün Ekleme
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromForm] UpdateProductWithImageDto dto)
        {
            var validationResult = await _createProductValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // ProductGroup kontrolü
            var productGroup = await _context.ProductGroups.FindAsync(dto.ProductGroupId);
            if (productGroup == null)
            {
                return BadRequest("Belirtilen ürün grubu bulunamadı.");
            }

            // Resim kaydetme işlemi
            string? imageUrl = null;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                var extension = Path.GetExtension(dto.ImageFile.FileName).ToLowerInvariant();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                    return BadRequest("Sadece .jpg, .jpeg veya .png dosyaları kabul edilir.");

                var fileName = $"{Guid.NewGuid()}{extension}";
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                var fullPath = Path.Combine(folderPath, fileName);

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(stream);
                }

                imageUrl = $"/images/{fileName}";
            }

            var product = new Product
            {
                Barcode = dto.Barcode,
                Name = dto.Name,
                Stock = dto.Stock,
                CriticalStockLevel = dto.CriticalStockLevel,
                SalePrice = dto.SalePrice,
                PurchasePrice = dto.PurchasePrice,
                TaxRate = dto.TaxRate,
                SalePageList = dto.SalePageList,
                ProductGroupId = dto.ProductGroupId,
                Unit = dto.Unit,
                OriginCountry = dto.OriginCountry,
                ImageUrl = imageUrl
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            if (dto.Stock > 0)
            {
                var stockTransaction = new StockTransaction
                {
                    ProductId = product.Id,
                    QuantityChange = dto.Stock,
                    TransactionType = "IN",
                    Reason = "First Stock Entry",
                    Date = DateTime.Now
                };

                _context.StockTransaction.Add(stockTransaction);
                await _context.SaveChangesAsync();
            }

            var response = new ProductResponseDto
            {
                Id = product.Id,
                Barcode = product.Barcode,
                Name = product.Name,
                Stock = product.Stock,
                SalePrice = product.SalePrice,
                PurchasePrice = product.PurchasePrice,
                ProfitRate = product.ProfitRate,
                TaxRate = product.TaxRate,
                ProductGroupId = product.ProductGroupId,
                ProductGroupName = productGroup.Name,
                ImageUrl = product.ImageUrl
            };

            return CreatedAtAction(
                actionName: nameof(GetProductByBarcode),
                routeValues: new { barcode = product.Barcode },
                value: response);
        }


        // Ürün Güncelleme
        [HttpPut("{id}")]        [Consumes("multipart/form-data")]        public async Task<ActionResult<ProductResponseDto>> UpdateProduct(int id, [FromForm] UpdateProductWithImageDto dto)        {            var validationResult = await _updateProductValidator.ValidateAsync(dto); // Doğru validator'ı kullanın            if (!validationResult.IsValid)            {                return BadRequest(validationResult.Errors);            }            var product = await _context.Products.Include(p => p.ProductGroup).FirstOrDefaultAsync(p => p.Id == id);             if (product == null)            {                return NotFound();            }                       if (product.ProductGroupId != dto.ProductGroupId)            {                var newProductGroup = await _context.ProductGroups.FindAsync(dto.ProductGroupId);                if (newProductGroup == null)                {                    return BadRequest("Belirtilen yeni ürün grubu bulunamadı.");                }                product.ProductGroup = newProductGroup;             }            product.Barcode = dto.Barcode;            product.Name = dto.Name;            product.Stock = dto.Stock;            product.CriticalStockLevel = dto.CriticalStockLevel;            product.SalePrice = dto.SalePrice;            product.PurchasePrice = dto.PurchasePrice;            product.TaxRate = dto.TaxRate;            product.SalePageList = dto.SalePageList;            product.ProductGroupId = dto.ProductGroupId; // ProductGroupId'yi güncelleyin            product.Unit = dto.Unit;            product.OriginCountry = dto.OriginCountry;            //  Resim güncelleme varsa            if (dto.ImageFile != null && dto.ImageFile.Length > 0)            {                var extension = Path.GetExtension(dto.ImageFile.FileName).ToLowerInvariant();                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")                    return BadRequest("Sadece .jpg, .jpeg veya .png dosyaları kabul edilir.");                // Eski dosyayı sil                if (!string.IsNullOrEmpty(product.ImageUrl))                {                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl.TrimStart('/'));                    if (System.IO.File.Exists(oldPath))                        System.IO.File.Delete(oldPath);                }                var fileName = $"{Guid.NewGuid()}{extension}";                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");                var fullPath = Path.Combine(folderPath, fileName);                if (!Directory.Exists(folderPath))                    Directory.CreateDirectory(folderPath);                using (var stream = new FileStream(fullPath, FileMode.Create))                {                    await dto.ImageFile.CopyToAsync(stream);                }                product.ImageUrl = $"/images/{fileName}";            }            await _context.SaveChangesAsync();            return Ok(new ProductResponseDto            {                Id = product.Id,                Barcode = product.Barcode,                Name = product.Name,                Stock = product.Stock,                SalePrice = product.SalePrice,                PurchasePrice = product.PurchasePrice,                ProfitRate = product.ProfitRate,                TaxRate = product.TaxRate,                ProductGroupId = product.ProductGroupId,                ProductGroupName = product.ProductGroup.Name             });        }        // Ürün Silme (burada değişiklik gerekmez)        [HttpDelete("{id}")]        public async Task<IActionResult> DeleteProduct(int id)        {            var product = await _context.Products.FindAsync(id);            if (product == null)            {                return NotFound();            }            _context.Products.Remove(product);            await _context.SaveChangesAsync();            return NoContent(); // 204        }        // ID ile Ürün Sorgulama        [HttpGet("id/{id}", Name = "GetProductById")]        public async Task<ActionResult<ProductResponseDto>> GetProductById(int id)        {            var product = await _context.Products.Include(p => p.ProductGroup).FirstOrDefaultAsync(p => p.Id == id);            if (product == null) return NotFound();            return Ok(new ProductResponseDto            {                Id = product.Id,                Barcode = product.Barcode,                Name = product.Name,                Stock = product.Stock,                SalePrice = product.SalePrice,                PurchasePrice = product.PurchasePrice,                ProfitRate = product.ProfitRate,                TaxRate = product.TaxRate,                ProductGroupId = product.ProductGroupId,                ProductGroupName = product.ProductGroup.Name // ProductGroup Adını dahil edin            });        }        // Barkod ile Ürün Sorgulama        [HttpGet("barcode/{barcode}", Name = "GetProductByBarcode")]        public async Task<ActionResult<ProductResponseDto>> GetProductByBarcode(string barcode)        {            var product = await _context.Products.Include(p => p.ProductGroup).FirstOrDefaultAsync(p => p.Barcode == barcode);            if (product == null) return NotFound();            return Ok(new ProductResponseDto            {                Id = product.Id,                Barcode = product.Barcode,                Name = product.Name,                Stock = product.Stock,                SalePrice = product.SalePrice,                PurchasePrice = product.PurchasePrice,                ProfitRate = product.ProfitRate,                TaxRate = product.TaxRate,                ProductGroupId = product.ProductGroupId,                ProductGroupName = product.ProductGroup.Name // ProductGroup Adını dahil edin            });        }    }}