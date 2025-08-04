using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class IncomeController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public IncomeController(MyPosDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddIncome([FromBody] TransactionDto dto)
    {
        var validator = new TransactionValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        if (dto.PaymentType != "NAKİT" && dto.PaymentType != "POS")
            return BadRequest("Invalid payment type for income.");

        var income = new Income
        {
            Amount = dto.Amount,
            Description = dto.Description,
            PaymentType = dto.PaymentType,
            TypeId = dto.TypeId,
            Date = dto.Date
        };

        _context.Incomes.Add(income);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Income added successfully." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var incomes = await _context.Incomes
            .Include(x => x.Type)
            .Select(x => new
            {
                x.Id,
                x.Amount,
                x.Description,
                x.PaymentType,
                x.Date,
                Type = x.Type.Name
            }).ToListAsync();

        return Ok(incomes);
    }

    // ✅ Güncelleme
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIncome(int id, [FromBody] TransactionDto dto)
    {
        var validator = new TransactionValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        var income = await _context.Incomes.FindAsync(id);
        if (income == null) return NotFound(new { message = "Income not found." });

        income.Amount = dto.Amount;
        income.Description = dto.Description;
        income.PaymentType = dto.PaymentType;
        income.TypeId = dto.TypeId;
        income.Date = dto.Date;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Income updated successfully." });
    }

    // ✅ Silme
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIncome(int id)
    {
        var income = await _context.Incomes.FindAsync(id);
        if (income == null) return NotFound(new { message = "Income not found." });

        _context.Incomes.Remove(income);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Income deleted successfully." });
    }
}
