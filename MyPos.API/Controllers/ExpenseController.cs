using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using System.Security.Claims; // Bu using'i ekle
using System.Linq; // Added for .ToList() and .Concat()
using System.Threading.Tasks; // Added for async/await

[Route("api/[controller]")]
[ApiController]
[Authorize] // Auth'u aç ve tüm controller için geçerli kıl
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
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
            Date = dto.Date,
            UserId = currentUserId // Kullanıcının ID'sini ekle
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Expense added successfully." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var expenses = await _context.Expenses
            .Where(e => e.UserId == currentUserId) // Sadece mevcut kullanıcıya ait giderleri getir
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

    // Güncelleme
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] TransactionDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var validator = new TransactionValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
        var expense = await _context.Expenses
                                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == currentUserId);

        if (expense == null) return NotFound(new { message = "Expense not found or you don't have permission." });

        expense.Amount = dto.Amount;
        expense.Description = dto.Description;
        expense.PaymentType = dto.PaymentType;
        expense.TypeId = dto.TypeId;
        expense.Date = dto.Date;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Expense updated successfully." });
    }

    // Silme
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
        var expense = await _context.Expenses
                                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == currentUserId);

        if (expense == null) return NotFound(new { message = "Expense not found or you don't have permission." });

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
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.Expenses
            .Include(e => e.Type)
            .Where(e => e.UserId == currentUserId) // Kullanıcı ID'ye göre ilk filtreleme
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

        var filteredExpenses = await query.Select(x => new
        {
            x.Id,
            x.Amount,
            x.Description,
            x.PaymentType,
            x.Date,
            Type = x.Type.Name
        }).ToListAsync();

        return Ok(new
        {
            Expenses = filteredExpenses
        });
    }

    [HttpGet("total-expense")]
    public async Task<IActionResult> GetTotalExpense()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece mevcut kullanıcıya ait giderleri hesapla
        var allExpenses = await _context.Expenses
                                        .Where(e => e.UserId == currentUserId)
                                        .ToListAsync();

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
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece mevcut kullanıcıya ait giderleri getir
        var expenseData = await _context.Expenses
            .Where(e => e.UserId == currentUserId)
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