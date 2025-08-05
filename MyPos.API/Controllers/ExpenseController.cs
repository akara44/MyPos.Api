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
    [HttpGet("filtered-list")]
    public async Task<IActionResult> GetFilteredExpenses(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] string? paymentType,
    [FromQuery] int? typeId)
    {
        var query = _context.Expenses
            .Include(e => e.Type) // Gider türü bilgisini çekmek için
            .AsQueryable();

        // Filtreleme mantığı aynı şekilde uygulanır
        if (startDate.HasValue)
        {
            query = query.Where(e => e.Date.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.Date.Date <= endDate.Value.Date);
        }

        if (!string.IsNullOrEmpty(paymentType))
        {
            query = query.Where(e => e.PaymentType == paymentType);
        }

        if (typeId.HasValue)
        {
            query = query.Where(e => e.TypeId == typeId.Value);
        }

        // Filtrelenmiş gider listesini al
        var filteredExpenses = await query.Select(x => new
        {
            x.Id,
            x.Amount,
            x.Description,
            x.PaymentType,
            x.Date,
            Type = x.Type.Name
        }).ToListAsync();

        // Toplam hesaplamaları da burada yapıp tek bir nesne içinde döndürebilirsiniz
        //var totalExpense = filteredExpenses.Sum(e => e.Amount);
        //var cashTotal = filteredExpenses.Where(e => e.PaymentType == "NAKİT").Sum(e => e.Amount);
        //var posCreditCardTotal = filteredExpenses.Where(e => e.PaymentType == "POS" || e.PaymentType == "KREDİ KARTI").Sum(e => e.Amount);

        return Ok(new
        {
            //TotalExpense = totalExpense,
            //CashTotal = cashTotal,
            //PosCreditCardTotal = posCreditCardTotal,
            Expenses = filteredExpenses // Filtrelenmiş listeyi de ekle
        });
    }
    [HttpGet("total-expense")]
    public async Task<IActionResult> GetTotalExpense()
    {
        var allExpenses = await _context.Expenses.ToListAsync();

        var totalExpense = allExpenses.Sum(e => e.Amount);

        var cashTotal = allExpenses
            .Where(e => e.PaymentType == "NAKİT")
            .Sum(e => e.Amount);

        var posCreditCardTotal = allExpenses
            .Where(e => e.PaymentType == "POS" || e.PaymentType == "KREDİ KARTI")
            .Sum(e => e.Amount);

        var totals = new
        {
            TotalExpense = totalExpense,
            CashTotal = cashTotal,
            PosCreditCardTotal = posCreditCardTotal
        };

        return Ok(totals);
    }
    [HttpGet("expense-by-type")]
    public async Task<IActionResult> GetExpenseByType()
    {
        var expenseData = await _context.Expenses
            .Include(e => e.Type)
            .ToListAsync();

        var groupedData = expenseData
            .GroupBy(e => e.Type.Name)
            .Select(g => new
            {
                Type = g.Key,
                TotalAmount = g.Sum(e => e.Amount)
            })
            .ToList();

        return Ok(groupedData);
    }
}
