using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ExpenseController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public ExpenseController(MyPosDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddExpense([FromBody] TransactionDto dto)
    {
        var validator = new TransactionValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        if (dto.PaymentType != "NAKİT" && dto.PaymentType != "KREDİ KARTI")
            return BadRequest("Invalid payment type for expense.");

        var expense = new Expense
        {
            Amount = dto.Amount,
            Description = dto.Description,
            PaymentType = dto.PaymentType,
            TypeId = dto.TypeId,
            Date = dto.Date
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Expense added successfully." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var expenses = await _context.Expenses
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

        return Ok(expenses);
    }

    // ✅ Güncelleme
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] TransactionDto dto)
    {
        var validator = new TransactionValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return NotFound(new { message = "Expense not found." });

        expense.Amount = dto.Amount;
        expense.Description = dto.Description;
        expense.PaymentType = dto.PaymentType;
        expense.TypeId = dto.TypeId;
        expense.Date = dto.Date;
       

        await _context.SaveChangesAsync();

        return Ok(new { message = "Expense updated successfully." });
    }

    // ✅ Silme
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return NotFound(new { message = "Expense not found." });

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Expense deleted successfully." });
    }
}
