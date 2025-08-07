// Controllers/OrderController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos; // Mevcut DTO'larınızın namespace'i
using MyPos.Infrastructure.Persistence; // DbContext'inizin namespace'i
using MyPos.Domain.Entities; // Modellerinizin namespace'i
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public OrderController(MyPosDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Belirli bir müşterinin sipariş listesini, tarih aralığına göre filtreleyerek döndürür.
    /// </summary>
    /// <param name="customerId">Müşteri ID'si</param>
    /// <param name="startDate">Filtreleme başlangıç tarihi (isteğe bağlı)</param>
    /// <param name="endDate">Filtreleme bitiş tarihi (isteğe bağlı)</param>
    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<OrderListDto>>> GetCustomerOrders(
        int customerId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
        if (!customerExists)
        {
            return NotFound("Belirtilen müşteri bulunamadı.");
        }

        var query = _context.Orders
            .Where(o => o.CustomerId == customerId);

        // Tarih filtrelemesi yap
        if (startDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= startDate.Value.Date);
        }
        if (endDate.HasValue)
        {
            // Bitiş gününün sonuna kadar olanları al
            query = query.Where(o => o.OrderDate <= endDate.Value.Date.AddDays(1).AddSeconds(-1));
        }

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderListDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                TotalDiscount = o.TotalDiscount,
                RemainingDebt = o.RemainingDebt,
                PaymentType = o.PaymentType,
                PersonnelName = "Örnek Personel" // Henüz personel modülü olmadığı için sabit değer.
            }).ToListAsync();

        return Ok(orders);
    }
}