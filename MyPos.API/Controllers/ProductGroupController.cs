using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;

namespace MyPos.API.Controllers;
[Route("api/[controller]")]
[ApiController]
//[Authorize]
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
        if (await _context.ProductGroups.AnyAsync(pg => pg.Name == dto.Name))
            return BadRequest("Bu isimde bir ürün grubu zaten var.");

        if (dto.ParentGroupId.HasValue)
        {
            var parentExists = await _context.ProductGroups.AnyAsync(pg => pg.Id == dto.ParentGroupId.Value);
            if (!parentExists)
                return BadRequest("Belirtilen üst grup bulunamadı.");
        }

        var productGroup = new ProductGroup
        {
            Name = dto.Name,
            ParentGroupId = dto.ParentGroupId
        };

        _context.ProductGroups.Add(productGroup);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Ürün grubu başarıyla eklendi." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var groups = await _context.ProductGroups
            .Select(pg => new { pg.Id, pg.Name })
            .ToListAsync();

        return Ok(groups);
    }

    // Güncelleme işlemi için PUT metodu
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProductGroup(int id, [FromBody] ProductGroupDto dto)
    {
        var productGroup = await _context.ProductGroups.FindAsync(id);
        if (productGroup == null)
            return NotFound("Ürün grubu bulunamadı.");

        if (await _context.ProductGroups.AnyAsync(pg => pg.Name == dto.Name && pg.Id != id))
            return BadRequest("Bu isimde başka bir ürün grubu zaten var.");

        if (dto.ParentGroupId.HasValue)
        {
            var parentExists = await _context.ProductGroups.AnyAsync(pg => pg.Id == dto.ParentGroupId.Value);
            if (!parentExists)
                return BadRequest("Belirtilen üst grup bulunamadı.");
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
        var productGroup = await _context.ProductGroups
            .Include(pg => pg.SubGroups)
            .FirstOrDefaultAsync(pg => pg.Id == id);

        if (productGroup == null)
            return NotFound("Ürün grubu bulunamadı.");

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
            .Where(p => p.ProductGroupId == id)
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
