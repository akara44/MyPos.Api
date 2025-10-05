
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Customers.Debts; // Yeni DTO'nuzun namespace'i
// MyPos.Domain.Entities.Debt; // Debt entity'niz için
using MyPos.Infrastructure.Persistence;
using System;
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

    // POST: api/debts
    /// <summary>
    /// Bir müşteriye yeni bir borç kaydı ekler ve müşterinin bakiyesini günceller.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDebt([FromBody] CreateDebtDto createDebtDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Müşteriyi bul ve yetki kontrolü yap
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == createDebtDto.CustomerId && c.UserId == currentUserId);

        if (customer == null)
        {
            return NotFound("Müşteri bulunamadı veya bu işlem için yetkiniz yok.");
        }

        // **Ödeme Controller'ındaki PaymentType kontrolüne benzer, burada ek bir kontrol şu anlık gerekmiyor.**

        // 2. Yeni bir Debt nesnesi oluştur
        var debt = new Debt // Yeni Debt Entity'niz
        {
            UserId = currentUserId,
            CustomerId = createDebtDto.CustomerId,
            Amount = createDebtDto.Amount,
            DebtDate = createDebtDto.DebtDate.ToUniversalTime(),
            Note = createDebtDto.Note
        };

        // 3. Müşterinin bakiyesini güncelle
        // Borç eklediğimiz için bakiye ARTAR.
        customer.Balance += debt.Amount;

        // 4. Veritabanına kaydet
        _context.Debts.Add(debt);
        await _context.SaveChangesAsync();

        // 5. Başarılı yanıt
        return StatusCode(201, new
        {
            message = $"Borç başarıyla eklendi. Yeni bakiye: {customer.Balance:C}",
            newBalance = customer.Balance // Yeni bakiyeyi frontend'e dönebiliriz.
        });
    }

    // Borçları listeleme metodu da buraya eklenebilir. (Örn: GET: api/customers/{customerId}/debts)
}