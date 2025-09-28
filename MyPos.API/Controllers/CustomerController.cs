using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Customers;
using MyPos.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // Bu using'i ekle
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Auth'u aç ve tüm controller için geçerli kıl
public class CustomerController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public CustomerController(MyPosDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Sadece mevcut kullanıcıya ait müşterileri listele
        var customers = await _context.Customers
                                        .Where(c => c.UserId == currentUserId)
                                        .ToListAsync();

        var customerDtos = customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            CustomerName = c.CustomerName,
            DueDate = c.DueDate,
            Phone = c.Phone,
            Address = c.Address,
            CustomerNote = c.CustomerNote,
            OpenAccountLimit = c.OpenAccountLimit,
            TaxOffice = c.TaxOffice,
            TaxNumber = c.TaxNumber
        }).ToList();

        return Ok(customerDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
        var customer = await _context.Customers
                                        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

        if (customer == null)
        {
            return NotFound("Müşteri bulunamadı veya yetkiniz yok.");
        }

        var customerDto = new CustomerDto
        {
            Id = customer.Id,
            CustomerName = customer.CustomerName,
            DueDate = customer.DueDate,
            Phone = customer.Phone,
            Address = customer.Address,
            CustomerNote = customer.CustomerNote,
            OpenAccountLimit = customer.OpenAccountLimit,
            TaxOffice = customer.TaxOffice,
            TaxNumber = customer.TaxNumber
        };

        return Ok(customerDto);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(CustomerDto customerDto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var validator = new CustomerValidator();
        var customer = new Customer
        {
            CustomerName = customerDto.CustomerName,
            DueDate = customerDto.DueDate,
            Phone = customerDto.Phone,
            Address = customerDto.Address,
            CustomerNote = customerDto.CustomerNote,
            OpenAccountLimit = customerDto.OpenAccountLimit,
            TaxOffice = customerDto.TaxOffice,
            TaxNumber = customerDto.TaxNumber,
            UserId = currentUserId // Oluşturulan veriye kullanıcının ID'sini ekle
        };

        var validationResult = validator.Validate(customer);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        customerDto.Id = customer.Id;

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customerDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, CustomerDto customerDto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (id != customerDto.Id)
        {
            return BadRequest("ID uyuşmuyor.");
        }

        // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
        var existingCustomer = await _context.Customers
                                                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

        if (existingCustomer == null)
        {
            return NotFound("Müşteri bulunamadı veya yetkiniz yok.");
        }

        existingCustomer.CustomerName = customerDto.CustomerName;
        existingCustomer.DueDate = customerDto.DueDate;
        existingCustomer.Phone = customerDto.Phone;
        existingCustomer.Address = customerDto.Address;
        existingCustomer.CustomerNote = customerDto.CustomerNote;
        existingCustomer.OpenAccountLimit = customerDto.OpenAccountLimit;
        existingCustomer.TaxOffice = customerDto.TaxOffice;
        existingCustomer.TaxNumber = customerDto.TaxNumber;

        var validator = new CustomerValidator();
        var validationResult = validator.Validate(existingCustomer);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Customers.Any(e => e.Id == id && e.UserId == currentUserId))
            {
                return NotFound("Müşteri bulunamadı veya yetkiniz yok.");
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Veriyi bulurken hem ID'yi hem de UserId'yi kontrol et
        var customer = await _context.Customers
                                        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

        if (customer == null)
        {
            return NotFound("Müşteri bulunamadı veya yetkiniz yok.");
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // CustomerController.cs içinde

    [HttpGet("{id}/summary")]
    public async Task<ActionResult<CustomerSummaryDto>> GetCustomerSummary(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // DÜZELTME: Müşterinin kendisini direkt çekiyoruz çünkü Balance bilgisi lazım.
        var customer = await _context.Customers
                                     .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

        if (customer == null)
        {
            return NotFound("Belirtilen müşteri bulunamadı veya yetkiniz yok.");
        }

        // DÜZELTME: Toplam satışı 'Orders' yerine 'Sales' tablosundan hesaplıyoruz.
        // Sadece tamamlanmış satışları dikkate alıyoruz.
        var totalSales = await _context.Sales
            .Where(s => s.CustomerId == id && s.IsCompleted)
            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

        // Toplam ödeme, müşterinin yaptığı tüm ödemelerin toplamıdır.
        // Bu kısım muhtemelen gelecekte bir ödeme ekranı yapınca daha anlamlı olacak.
        var totalPayment = await _context.Payments
            .Where(p => p.CustomerId == id)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        // DÜZELTME: Kalan bakiye için artık sıfırdan hesaplama yapmıyoruz.
        // Direkt olarak Customer entity'sine eklediğimiz Balance alanını kullanıyoruz. BU ÇOK DAHA HIZLI!
        var remainingBalance = customer.Balance;

        var summaryDto = new CustomerSummaryDto
        {
            TotalSales = totalSales,
            TotalPayment = totalPayment,
            RemainingBalance = remainingBalance
        };

        return Ok(summaryDto);
    }
    // CustomerController.cs içinde

    // CustomerController.cs içinde

    [HttpGet("{id}/sales")]
    public async Task<IActionResult> GetCustomerSales(int id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Müşterinin kendisini ve güncel Balance bilgisini en başta bir kere çekiyoruz.
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);
        if (customer == null)
            return NotFound("Müşteri bulunamadı veya yetkiniz yok.");

        var query = _context.Sales
            .Where(s => s.CustomerId == id && s.IsCompleted);

        if (startDate.HasValue)
            query = query.Where(s => s.SaleDate.Date >= startDate.Value.Date);
        if (endDate.HasValue)
            query = query.Where(s => s.SaleDate.Date <= endDate.Value.Date);

        var salesList = await query
            .OrderByDescending(s => s.SaleDate) // Listenin yeniden eskiye sıralandığından emin ol
            .Select(s => new CustomerSaleListDto
            {
                SaleId = s.SaleId,
                SaleCode = s.SaleCode,
                TotalQuantity = s.TotalQuantity,
                DiscountRate = (s.SubTotalAmount > 0) ? Math.Round((s.DiscountAmount / s.SubTotalAmount * 100), 2) : 0,
                TotalAmount = s.TotalAmount,

                // DEĞİŞİKLİK: Her satır için müşterinin GÜNCEL toplam borcunu basıyoruz.
                RemainingDebt = customer.Balance,

                PaymentType = s.PaymentType,
                PersonnelName = "Admin",
                Date = s.SaleDate.ToString("dd.MM.yyyy"),
                Time = s.SaleDate.ToString("HH:mm")
            }).ToListAsync();

        return Ok(salesList);
    }
}