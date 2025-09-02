using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using System;

[Route("api/[controller]")]
[ApiController]
//[Authorize]
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
        var types = await _context.ExpenseIncomeTypes
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        return Ok(types);
    }

    //  api/ExpenseIncomeType
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ExpenseIncomeTypeDto dto)
    {
        var validator = new ExpenseIncomeTypeValidator();
        var result = validator.Validate(dto);

        if (!result.IsValid)
            return BadRequest(result.Errors);

        var entity = new ExpenseIncomeType { Name = dto.Name };

        await _context.ExpenseIncomeTypes.AddAsync(entity);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Type added successfully." });
    }

    //  api/ExpenseIncomeType/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ExpenseIncomeTypeDto dto)
    {
        var validator = new ExpenseIncomeTypeValidator();
        var result = validator.Validate(dto);

        if (!result.IsValid)
            return BadRequest(result.Errors);

        var entity = await _context.ExpenseIncomeTypes.FindAsync(id);
        if (entity == null)
            return NotFound();

        entity.Name = dto.Name;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Type updated successfully." });
    }

    //  api/ExpenseIncomeType/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.ExpenseIncomeTypes.FindAsync(id);
        if (entity == null)
            return NotFound();

        _context.ExpenseIncomeTypes.Remove(entity);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Type deleted successfully." });
    }
}
