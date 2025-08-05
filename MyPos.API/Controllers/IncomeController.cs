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
    [HttpGet("filtered-list")]
    public async Task<IActionResult> GetFilteredIncomes(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] string? paymentType,
    [FromQuery] int? typeId)
    {
        var query = _context.Incomes
            .Include(x => x.Type) // Type bilgisini de çekmek için
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

        // Filtrelenmiş gelir listesini al
        var filteredIncomes = await query.Select(x => new
        {
            x.Id,
            x.Amount,
            x.Description,
            x.PaymentType,
            x.Date,
            Type = x.Type.Name
        }).ToListAsync();

        // Toplam hesaplamaları da burada yapıp tek bir nesne içinde döndürebilirsiniz
        //var totalIncome = filteredIncomes.Sum(i => i.Amount);
        //var cashTotal = filteredIncomes.Where(i => i.PaymentType == "NAKİT").Sum(i => i.Amount);
        //var posTotal = filteredIncomes.Where(i => i.PaymentType == "POS").Sum(i => i.Amount);

        return Ok(new
        {
            //TotalIncome = totalIncome,
            //CashTotal = cashTotal,
            //PosTotal = posTotal,
            Incomes = filteredIncomes // Filtrelenmiş listeyi de ekle
        });
    }
    [HttpGet("total-income")]
    public async Task<IActionResult> GetTotalIncome()
    {
        // Veritabanındaki tüm gelirleri çekiyoruz
        var allIncomes = await _context.Incomes.ToListAsync();

        // Toplam gelirleri hesaplıyoruz
        var totalIncome = allIncomes.Sum(i => i.Amount);

        // Nakit gelirlerin toplamını hesaplıyoruz
        var cashTotal = allIncomes
            .Where(i => i.PaymentType == "NAKİT")
            .Sum(i => i.Amount);

        // POS gelirlerinin toplamını hesaplıyoruz (kredi kartı da dahil)
        var posTotal = allIncomes
            .Where(i => i.PaymentType == "POS" || i.PaymentType == "KREDİ KARTI")
            .Sum(i => i.Amount);

        // Sonuçları tek bir nesne olarak döndürüyoruz
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
        // Veritabanındaki tüm gelirleri tür bilgisiyle birlikte çekiyoruz
        // .Include(i => i.Type) ile Income'a bağlı olan ExpenseIncomeType bilgisini de alıyoruz.
        var incomeData = await _context.Incomes
            .Include(i => i.Type)
            .ToListAsync();

        // Verileri gruplara ayırıp her grubun toplamını hesaplıyoruz
        var groupedData = incomeData
            .GroupBy(i => i.Type.Name) // Gelir türünün adına göre gruplama yapıyoruz
            .Select(g => new
            {
                Type = g.Key, // Grubun adı (örneğin: "Alacak", "Borç")
                TotalAmount = g.Sum(i => i.Amount) // Bu gruba ait toplam miktar
            })
            .ToList();

        // Sonuçları JSON formatında döndürüyoruz
        // Bu veri, frontend'de grafik çizmek için kullanılacak
        return Ok(groupedData);
    }
}
