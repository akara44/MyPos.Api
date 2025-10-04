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

        // 2. YENİ KONTROL: Ödeme Tipinin ID'si (PaymentTypeId) geçerli mi?
        var paymentTypeExists = await _context.PaymentTypes
            .AnyAsync(pt => pt.Id == createPaymentDto.PaymentTypeId && pt.UserId == currentUserId);

        if (!paymentTypeExists)
        {
            return BadRequest("Geçersiz Ödeme Tipi ID'si. Lütfen tanımlı ödeme tiplerinden birini seçin.");
        }

        // 3. Yeni bir Payment nesnesi oluştur
        var payment = new Payment
        {
            UserId = currentUserId,
            CustomerId = createPaymentDto.CustomerId,
            Amount = createPaymentDto.Amount,
            PaymentDate = createPaymentDto.PaymentDate.ToUniversalTime(),
            PaymentTypeId = createPaymentDto.PaymentTypeId, // ARTIK ID KULLANILIYOR

            Note = createPaymentDto.Note
        };

        // 4. Müşterinin bakiyesini güncelle
        customer.Balance -= payment.Amount;

        // 5. Veritabanına kaydet
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

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

        // Müşteri kontrolü
        var customerExists = await _context.Customers
            .AnyAsync(c => c.Id == customerId && c.UserId == currentUserId);

        if (!customerExists)
        {
            return NotFound("Müşteri bulunamadı veya bu işlem için yetkiniz yok.");
        }

        var payments = await _context.Payments
            .Include(p => p.PaymentType) // PaymentType verisini çekmeyi dahil et
            .Where(p => p.CustomerId == customerId && p.UserId == currentUserId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentListDto
            {
                Id = p.Id,
                Amount = p.Amount,
                PaymentType = p.PaymentType.Name, // İlişkili PaymentType tablosundaki adı çek
                Date = p.PaymentDate.ToLocalTime().ToString("dd.MM.yyyy"),
                Time = p.PaymentDate.ToLocalTime().ToString("HH:mm"),
                Note = p.Note
            })
            .ToListAsync();

        return Ok(payments);
    }
}