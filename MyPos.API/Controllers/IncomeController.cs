using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Infrastructure.Persistence;
using System;
using System.Linq;
using System.Security.Claims; // Bu using'i ekle
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Auth'u aç ve tüm controller için geçerli kıl
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
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
            Date = dto.Date,
            UserId = currentUserId // Kullanıcının ID'sini ekle
        };

        _context.Incomes.Add(income);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Income added successfully." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece mevcut kullanıcıya ait gelirleri getir
        var incomes = await _context.Incomes
            .Where(i => i.UserId == currentUserId)
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

    // Güncelleme
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIncome(int id, [FromBody] TransactionDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var validator = new TransactionValidator();
        var result = validator.Validate(dto);
        if (!result.IsValid) return BadRequest(result.Errors);

        // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
        var income = await _context.Incomes
                                   .FirstOrDefaultAsync(i => i.Id == id && i.UserId == currentUserId);

        if (income == null) return NotFound(new { message = "Income not found or you don't have permission." });

        income.Amount = dto.Amount;
        income.Description = dto.Description;
        income.PaymentType = dto.PaymentType;
        income.TypeId = dto.TypeId;
        income.Date = dto.Date;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Income updated successfully." });
    }

    // Silme
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIncome(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
        var income = await _context.Incomes
                                   .FirstOrDefaultAsync(i => i.Id == id && i.UserId == currentUserId);

        if (income == null) return NotFound(new { message = "Income not found or you don't have permission." });

        _context.Incomes.Remove(income);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Income deleted successfully." });
    }

    [HttpGet("filtered-list")]
    public async Task<IActionResult> GetFilteredIncomes(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] string? paymentType,
    [FromQuery] int? typeId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.Incomes
            .Where(i => i.UserId == currentUserId) // Kullanıcı ID'ye göre ilk filtreleme
            .Include(x => x.Type)
            .AsQueryable();

        // Filtreleme mantığı aynı şekilde uygulanır
        if (startDate.HasValue)
        {
            query = query.Where(i => i.Date.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(i => i.Date.Date <= endDate.Value.Date);
        }

        if (!string.IsNullOrEmpty(paymentType))
        {
            query = query.Where(i => i.PaymentType == paymentType);
        }

        if (typeId.HasValue)
        {
            query = query.Where(i => i.TypeId == typeId.Value);
        }

        var filteredIncomes = await query.Select(x => new
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
            Incomes = filteredIncomes
        });
    }

    [HttpGet("total-income")]
    public async Task<IActionResult> GetTotalIncome()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece mevcut kullanıcıya ait gelirleri çekiyoruz
        var allIncomes = await _context.Incomes
                                       .Where(i => i.UserId == currentUserId)
                                       .ToListAsync();

        var totalIncome = allIncomes.Sum(i => i.Amount);

        var cashTotal = allIncomes
            .Where(i => i.PaymentType == "NAKİT")
            .Sum(i => i.Amount);

        var posTotal = allIncomes
            .Where(i => i.PaymentType == "POS" || i.PaymentType == "KREDİ KARTI")
            .Sum(i => i.Amount);

        var totals = new
        {
            TotalIncome = totalIncome,
            CashTotal = cashTotal,
            PosTotal = posTotal
        };

        return Ok(totals);
    }

    [HttpGet("income-by-type")]
    public async Task<IActionResult> GetIncomeByType()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece mevcut kullanıcıya ait gelirleri tür bilgisiyle birlikte çekiyoruz
        var incomeData = await _context.Incomes
            .Where(i => i.UserId == currentUserId)
            .Include(i => i.Type)
            .ToListAsync();

        var groupedData = incomeData
            .GroupBy(i => i.Type.Name)
            .Select(g => new
            {
                Type = g.Key,
                TotalAmount = g.Sum(i => i.Amount)
            })
            .ToList();

        return Ok(groupedData);
    }
}