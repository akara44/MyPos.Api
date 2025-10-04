using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Payments;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public PaymentsController(MyPosDbContext context)
    {
        _context = context;
    }

    // POST: api/payments
    /// <summary>
    /// Bir müşteri için yeni bir ödeme (tahsilat) oluşturur ve müşterinin bakiyesini günceller.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto createPaymentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Müşteriyi bul ve yetki kontrolü yap
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == createPaymentDto.CustomerId && c.UserId == currentUserId);

        if (customer == null)
        {
            return NotFound("Müşteri bulunamadı veya bu işlem için yetkiniz yok.");
        }

        // 2. Yeni bir Payment nesnesi oluştur
        var payment = new Payment
        {
            UserId = currentUserId,
            CustomerId = createPaymentDto.CustomerId,
            Amount = createPaymentDto.Amount,
            PaymentDate = createPaymentDto.PaymentDate.ToUniversalTime(),
            PaymentType = createPaymentDto.PaymentType,

            Note = createPaymentDto.Note
        };

        // 3. Müşterinin bakiyesini güncelle (Yapılan ödeme borçtan düşülür)
        customer.Balance -= payment.Amount;

        // 4. Veritabanına kaydet
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // 201 Created durum kodu ile birlikte oluşturulan kaynağı (veya bir onay mesajı) dön
        return StatusCode(201, new { message = "Ödeme başarıyla eklendi." });
    }

    // GET: api/customers/{customerId}/payments
    /// <summary>
    /// Belirtilen müşteriye ait tüm ödemeleri listeler.
    /// </summary>
    [HttpGet("/api/customers/{customerId}/payments")]
    public async Task<ActionResult<IEnumerable<PaymentListDto>>> GetPaymentsForCustomer(int customerId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Müşterinin varlığını ve kullanıcıya ait olduğunu kontrol et
        var customerExists = await _context.Customers
            .AnyAsync(c => c.Id == customerId && c.UserId == currentUserId);

        if (!customerExists)
        {
            return NotFound("Müşteri bulunamadı veya bu işlem için yetkiniz yok.");
        }

        var payments = await _context.Payments
            .Where(p => p.CustomerId == customerId && p.UserId == currentUserId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentListDto
            {
                Id = p.Id,
                Amount = p.Amount,
                PaymentType = p.PaymentType,
                Date = p.PaymentDate.ToLocalTime().ToString("dd.MM.yyyy"),
                Time = p.PaymentDate.ToLocalTime().ToString("HH:mm"),
                Note = p.Note
            })
            .ToListAsync();

        return Ok(payments);
    }
}