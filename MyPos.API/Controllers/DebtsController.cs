
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Customers.Debts;
using MyPos.Domain.Entities; // Debt ve Customer entity'leri için
using MyPos.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DebtsController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public DebtsController(MyPosDbContext context)
    {
        _context = context;
    }

    // =================================================================
    // POST: api/debts (CREATE)
    // =================================================================
    [HttpPost]
    public async Task<IActionResult> CreateDebt([FromBody] CreateDebtDto createDebtDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == createDebtDto.CustomerId && c.UserId == currentUserId);

        if (customer == null)
        {
            return NotFound("Müşteri bulunamadı veya bu işlem için yetkiniz yok.");
        }

        var debt = new Debt
        {
            UserId = currentUserId,
            CustomerId = createDebtDto.CustomerId,
            Amount = createDebtDto.Amount,
            DebtDate = createDebtDto.DebtDate.ToUniversalTime(),
            Note = createDebtDto.Note
        };

        // Bakiye güncelleme: Borç eklendiği için bakiye ARTAR.
        customer.Balance += debt.Amount;

        _context.Debts.Add(debt);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            message = $"Borç başarıyla eklendi. Yeni bakiye: {customer.Balance}",
            newBalance = customer.Balance
        });
    }

    // =================================================================
    // GET: api/customers/{customerId}/debts (LIST)
    // =================================================================
    /// <summary>
    /// Belirtilen müşteriye ait tüm borç kayıtlarını listeler.
    /// </summary>
    [HttpGet("/api/customers/{customerId}/debts")]
    public async Task<ActionResult<IEnumerable<DebtListDto>>> GetDebtsForCustomer(int customerId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Müşteri kontrolü
        var customerExists = await _context.Customers
            .AnyAsync(c => c.Id == customerId && c.UserId == currentUserId);

        if (!customerExists)
        {
            return NotFound("Müşteri bulunamadı veya bu işlem için yetkiniz yok.");
        }

        var debts = await _context.Debts
            .Where(d => d.CustomerId == customerId && d.UserId == currentUserId)
            .OrderByDescending(d => d.DebtDate)
            .Select(d => new DebtListDto
            {
                Id = d.Id,
                Amount = d.Amount,
                Date = d.DebtDate.ToLocalTime().ToString("dd.MM.yyyy"),
                Time = d.DebtDate.ToLocalTime().ToString("HH:mm"),
                Note = d.Note
            })
            .ToListAsync();

        return Ok(debts);
    }

    // =================================================================
    // GET: api/debts/{id} (SINGLE READ)
    // =================================================================
    /// <summary>
    /// Tek bir borç kaydının detayını getirir.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UpdateDebtDto>> GetDebt(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var debt = await _context.Debts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == currentUserId);

        if (debt == null)
        {
            return NotFound("Borç kaydı bulunamadı veya yetkiniz yok.");
        }

        var debtDto = new UpdateDebtDto
        {
            Id = debt.Id,
            CustomerId = debt.CustomerId,
            Amount = debt.Amount,
            DebtDate = debt.DebtDate.ToLocalTime(),
            Note = debt.Note
        };

        return Ok(debtDto);
    }


    // =================================================================
    // PUT: api/debts/{id} (UPDATE)
    // =================================================================
    /// <summary>
    /// Borç kaydını günceller ve müşteri bakiyesini ayarlar.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDebt(int id, UpdateDebtDto updateDto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (id != updateDto.Id) // ID kontrolü
        {
            return BadRequest("ID uyuşmuyor.");
        }

        var existingDebt = await _context.Debts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == currentUserId && d.CustomerId == updateDto.CustomerId);

        if (existingDebt == null)
        {
            return NotFound("Borç kaydı bulunamadı veya yetkiniz yok.");
        }

        var oldAmount = existingDebt.Amount;
        var newAmount = updateDto.Amount;

        // 1. Müşteriyi çek
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == updateDto.CustomerId && c.UserId == currentUserId);

        if (customer == null)
        {
            return BadRequest("İlişkili müşteri bulunamadı.");
        }

        // 2. Bakiyeyi güncelleme mantığı:
        // Önce eski borç miktarını bakiyeden çıkar (iade etme)
        customer.Balance -= oldAmount;
        // Sonra yeni borç miktarını bakiyeye ekle
        customer.Balance += newAmount;

        // 3. Debt kaydını güncelle
        existingDebt.Amount = newAmount;
        existingDebt.DebtDate = updateDto.DebtDate.ToUniversalTime();
        existingDebt.Note = updateDto.Note;

        try // Hata yakalama ve kontrol
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Debts.Any(e => e.Id == id && e.UserId == currentUserId))
            {
                return NotFound("Borç kaydı bulunamadı veya yetkiniz yok.");
            }
            else
            {
                throw;
            }
        }

        return NoContent(); // 204
    }

    // =================================================================
    // DELETE: api/debts/{id} (DELETE)
    // =================================================================
    /// <summary>
    /// Borç kaydını siler ve müşteri bakiyesini günceller.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDebt(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var debtToDelete = await _context.Debts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == currentUserId);

        if (debtToDelete == null)
        {
            return NotFound("Borç kaydı bulunamadı veya yetkiniz yok.");
        }

        // 1. İlişkili müşteriyi bul
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == debtToDelete.CustomerId && c.UserId == currentUserId);

        if (customer != null)
        {
            // 2. Müşteri bakiyesini güncelle
            // Silinen borç miktarı bakiyeden ÇIKARILIR.
            customer.Balance -= debtToDelete.Amount;
        }

        // 3. Borç kaydını sil
        _context.Debts.Remove(debtToDelete);
        await _context.SaveChangesAsync();

        return NoContent(); // 204
    }
}