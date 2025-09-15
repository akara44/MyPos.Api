using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Product;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace MyPos.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Auth'u aç ve tüm controller için geçerli kıl
public class ProductGroupController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public ProductGroupController(MyPosDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddProductGroup([FromBody] ProductGroupDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Mevcut kullanıcıya ait aynı isimde bir grup var mı kontrol et
        if (await _context.ProductGroups.AnyAsync(pg => pg.Name == dto.Name && pg.UserId == currentUserId))
            return BadRequest("Bu isimde bir ürün grubu zaten var.");

        if (dto.ParentGroupId.HasValue)
        {
            // Üst grubun mevcut kullanıcıya ait olup olmadığını kontrol et
            var parentExists = await _context.ProductGroups
                .AnyAsync(pg => pg.Id == dto.ParentGroupId.Value && pg.UserId == currentUserId);
            if (!parentExists)
                return BadRequest("Belirtilen üst grup bulunamadı veya yetkiniz yok.");
        }

        var productGroup = new ProductGroup
        {
            Name = dto.Name,
            ParentGroupId = dto.ParentGroupId,
            UserId = currentUserId // Kullanıcı ID'sini ekle
        };

        _context.ProductGroups.Add(productGroup);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Ürün grubu başarıyla eklendi." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece mevcut kullanıcıya ait grupları getir
        var groups = await _context.ProductGroups
            .Where(pg => pg.UserId == currentUserId)
            .Select(pg => new { pg.Id, pg.Name, pg.ParentGroupId }) // ParentGroupId'yi de çekmek faydalı olabilir
            .ToListAsync();

        return Ok(groups);
    }

    // Güncelleme işlemi için PUT metodu
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProductGroup(int id, [FromBody] ProductGroupDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Ürün grubunu bulurken hem ID hem de UserId'yi kontrol et
        var productGroup = await _context.ProductGroups
            .FirstOrDefaultAsync(pg => pg.Id == id && pg.UserId == currentUserId);

        if (productGroup == null)
            return NotFound("Ürün grubu bulunamadı veya yetkiniz yok.");

        // Mevcut kullanıcıya ait aynı isimde başka bir grup var mı kontrol et
        if (await _context.ProductGroups.AnyAsync(pg => pg.Name == dto.Name && pg.Id != id && pg.UserId == currentUserId))
            return BadRequest("Bu isimde başka bir ürün grubu zaten var.");

        if (dto.ParentGroupId.HasValue)
        {
            // Yeni üst grubun mevcut kullanıcıya ait olup olmadığını kontrol et
            var parentExists = await _context.ProductGroups
                .AnyAsync(pg => pg.Id == dto.ParentGroupId.Value && pg.UserId == currentUserId);
            if (!parentExists)
                return BadRequest("Belirtilen üst grup bulunamadı veya yetkiniz yok.");
        }

        productGroup.Name = dto.Name;
        productGroup.ParentGroupId = dto.ParentGroupId;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Ürün grubu başarıyla güncellendi." });
    }

    // Silme işlemi için DELETE metodu
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProductGroup(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Ürün grubunu bulurken hem ID hem de UserId'yi kontrol et
        var productGroup = await _context.ProductGroups
            .Include(pg => pg.SubGroups)
            .FirstOrDefaultAsync(pg => pg.Id == id && pg.UserId == currentUserId);

        if (productGroup == null)
            return NotFound("Ürün grubu bulunamadı veya yetkiniz yok.");

        // Alt grupların ParentGroupId'sini null yap
        if (productGroup.SubGroups != null && productGroup.SubGroups.Any())
        {
            foreach (var subGroup in productGroup.SubGroups)
            {
                subGroup.ParentGroupId = null;
            }
        }

        // Bu gruba bağlı ürünlerin ProductGroupId'sini null yap
        var products = await _context.Products
            .Where(p => p.ProductGroupId == id && p.UserId == currentUserId) // Ürünleri de UserId'ye göre filtrele
            .ToListAsync();

        foreach (var product in products)
        {
            product.ProductGroupId = null;
        }

        // Grubu sil
        _context.ProductGroups.Remove(productGroup);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Ürün grubu ve bağlantıları başarıyla güncellendi." });
    }
}