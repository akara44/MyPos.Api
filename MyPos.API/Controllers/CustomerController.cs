using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CustomerController : ControllerBase
{
    // Veritabanı bağlamını (DbContext) kullanmak için bir alan tanımlıyoruz.
    private readonly MyPosDbContext _context;

    
    public CustomerController(MyPosDbContext context)
    {
        _context = context;
    }

    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
    {
        
        var customers = await _context.Customers.ToListAsync();

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
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
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
            TaxNumber = customerDto.TaxNumber
        };

        var validationResult = validator.Validate(customer);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(); // Değişiklikleri veritabanına kaydet.

        customerDto.Id = customer.Id; 

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customerDto);
    }

    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, CustomerDto customerDto)
    {
        if (id != customerDto.Id)
        {
            return BadRequest("ID uyuşmuyor.");
        }

        var existingCustomer = await _context.Customers.FindAsync(id);
        if (existingCustomer == null)
        {
            return NotFound();
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
            if (!_context.Customers.Any(e => e.Id == id))
            {
                return NotFound();
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
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(); // Değişikliği veritabanına kaydet.

        return NoContent();
    }
    [HttpGet("{id}/summary")]
    public async Task<ActionResult<CustomerSummaryDto>> GetCustomerSummary(int id)
    {
        // Müşterinin varlığını kontrol et, yoksa NotFound dönsün.
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == id);
        if (!customerExists)
        {
            return NotFound("Belirtilen müşteri bulunamadı.");
        }

        // Müşterinin toplam satış tutarını hesapla
        var totalSales = await _context.Orders
            .Where(o => o.CustomerId == id)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        // Müşterinin toplam ödeme tutarını hesapla
        var totalPayment = await _context.Payments
            .Where(p => p.CustomerId == id)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        // Kalan bakiyeyi hesapla
        var remainingBalance = totalSales - totalPayment;

        var summaryDto = new CustomerSummaryDto
        {
            TotalSales = totalSales,
            TotalPayment = totalPayment,
            RemainingBalance = remainingBalance
        };

        return Ok(summaryDto);
    }
}

