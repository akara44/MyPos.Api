using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Customers.Payments;
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
        // ... (Mevcut CreatePayment metodunuz burada devam ediyor)
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
            PaymentTypeId = createPaymentDto.PaymentTypeId,
            Note = createPaymentDto.Note
        };

        // 4. Müşterinin bakiyesini güncelle (Ödeme geldiği için borç azalır)
        customer.Balance -= payment.Amount;

        // 5. Veritabanına kaydet
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return StatusCode(201, new { message = "Ödeme başarıyla eklendi." });
    }

    // ======================================================================
    // YENİ ENDPOINT: PUT - Ödeme Güncelleme
    // ======================================================================
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] UpdatePaymentDto updatePaymentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Ödeme kaydını bul
        var payment = await _context.Payments
            .Include(p => p.Customer) // Müşteriyi de dahil et
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == currentUserId);

        if (payment == null)
        {
            return NotFound("Ödeme kaydı bulunamadı veya yetkiniz yok.");
        }

        // 2. Ödeme Tipinin geçerliliğini kontrol et
        var paymentTypeExists = await _context.PaymentTypes
            .AnyAsync(pt => pt.Id == updatePaymentDto.PaymentTypeId && pt.UserId == currentUserId);

        if (!paymentTypeExists)
        {
            return BadRequest("Geçersiz Ödeme Tipi ID'si.");
        }

        var originalAmount = payment.Amount;
        var newAmount = updatePaymentDto.Amount;

        // 3. Müşteri Bakiyesini Güncelleme Mantığı (KRİTİK KISIM)
        // Yeni Bakiyeyi Hesapla = Eski Bakiye + (Eski Ödeme Miktarı - Yeni Ödeme Miktarı)
        // (Eski ödemeyi iptal edip, yeni ödemeyi uygular)
        payment.Customer.Balance += originalAmount; // Eski tahsilatı iptal et (borcu artır)
        payment.Customer.Balance -= newAmount;      // Yeni tahsilatı uygula (borcu azalt)

        // 4. Ödeme nesnesini güncelle
        payment.Amount = newAmount;
        payment.PaymentDate = updatePaymentDto.PaymentDate.ToUniversalTime();
        payment.PaymentTypeId = updatePaymentDto.PaymentTypeId;
        payment.Note = updatePaymentDto.Note;

        try
        {
            await _context.SaveChangesAsync();
            return Ok(new { message = "Ödeme başarıyla güncellendi.", newBalance = payment.Customer.Balance });
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(500, "Veritabanı güncelleme hatası oluştu.");
        }
    }

    // ======================================================================
    // YENİ ENDPOINT: DELETE - Ödeme Silme
    // ======================================================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Ödeme kaydını bul
        var payment = await _context.Payments
            .Include(p => p.Customer) // Müşteriyi de dahil et
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == currentUserId);

        if (payment == null)
        {
            return NotFound("Ödeme kaydı bulunamadı veya yetkiniz yok.");
        }

        // 2. Müşteri Bakiyesini Güncelleme Mantığı (KRİTİK KISIM)
        // Ödeme silindiği için, müşterinin borcu (Balance) silinen ödeme miktarı kadar ARTIRILMALIDIR.
        payment.Customer.Balance += payment.Amount;

        // 3. Veritabanından sil
        _context.Payments.Remove(payment);

        try
        {
            await _context.SaveChangesAsync();
            return Ok(new { message = "Ödeme başarıyla silindi.", newBalance = payment.Customer.Balance });
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(500, "Veritabanı silme hatası oluştu.");
        }
    }


    // GET: api/customers/{customerId}/payments
    /// <summary>
    /// Belirtilen müşteriye ait tüm ödemeleri listeler.
    /// </summary>
    [HttpGet("/api/customers/{customerId}/payments")]
    public async Task<ActionResult<IEnumerable<PaymentListDto>>> GetPaymentsForCustomer(int customerId)
    {
        // ... (Mevcut GetPaymentsForCustomer metodunuz burada devam ediyor)
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