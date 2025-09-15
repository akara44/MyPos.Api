using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using System;
using System.Security.Claims; // Bu using'i ekle
using System.Linq; // Bu using'i ekle
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Auth'u aç
public class ExpenseIncomeTypeController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public ExpenseIncomeTypeController(MyPosDbContext context)
    {
        _context = context;
    }

    // api/ExpenseIncomeType
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece mevcut kullanıcıya ait tipleri getir
        var types = await _context.ExpenseIncomeTypes
            .Where(x => x.UserId == currentUserId)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return Ok(types);
    }

    // api/ExpenseIncomeType
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ExpenseIncomeTypeDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var validator = new ExpenseIncomeTypeValidator();
        var result = validator.Validate(dto);

        if (!result.IsValid)
            return BadRequest(result.Errors);

        var entity = new ExpenseIncomeType
        {
            Name = dto.Name,
            UserId = currentUserId // Kullanıcının ID'sini ekle
        };

        await _context.ExpenseIncomeTypes.AddAsync(entity);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Type added successfully." });
    }

    // api/ExpenseIncomeType/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ExpenseIncomeTypeDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var validator = new ExpenseIncomeTypeValidator();
        var result = validator.Validate(dto);

        if (!result.IsValid)
            return BadRequest(result.Errors);

        // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
        var entity = await _context.ExpenseIncomeTypes
                                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == currentUserId);

        if (entity == null)
            return NotFound();

        entity.Name = dto.Name;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Type updated successfully." });
    }

    // api/ExpenseIncomeType/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
        var entity = await _context.ExpenseIncomeTypes
                                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == currentUserId);

        if (entity == null)
            return NotFound();

        _context.ExpenseIncomeTypes.Remove(entity);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Type deleted successfully." });
    }
}