using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Customers; // Bu using'in doğru olduğundan emin ol
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System; // DateTime için eklendi
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly MyPosDbContext _context;

    public CustomerController(MyPosDbContext context)
    {
        _context = context;
    }

    // --- DEĞİŞİKLİK 1: GET Metodları Artık "GetCustomerDto" Kullanıyor ---
    // Bu, müşterinin tüm detaylı bilgilerini frontend'e göndermemizi sağlar.

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetCustomerDto>>> GetCustomers()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var customers = await _context.Customers
            .Where(c => c.UserId == currentUserId)
            .Select(c => new GetCustomerDto
            {
                Id = c.Id,
                CustomerType = c.CustomerType,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                CustomerLastName = c.CustomerLastName,
                Email = c.Email,
                Phone = c.Phone,
                Country = c.Country,
                City = c.City,
                District = c.District,
                Address = c.Address,
                PostalCode = c.PostalCode,
                TaxOffice = c.TaxOffice,
                TaxNumber = c.TaxNumber,
                OpenAccountLimit = c.OpenAccountLimit,
                DueDateInDays = c.DueDateInDays,
                Discount = c.Discount,
                PriceType = c.PriceType,
                CustomerNote = c.CustomerNote,
                Balance = c.Balance
            })
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GetCustomerDto>> GetCustomer(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

        if (customer == null)
        {
            return NotFound("Müşteri bulunamadı veya yetkiniz yok.");
        }

        var customerDto = new GetCustomerDto
        {
            Id = customer.Id,
            CustomerType = customer.CustomerType,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            CustomerLastName = customer.CustomerLastName,
            Email = customer.Email,
            Phone = customer.Phone,
            Country = customer.Country,
            City = customer.City,
            District = customer.District,
            Address = customer.Address,
            PostalCode = customer.PostalCode,
            TaxOffice = customer.TaxOffice,
            TaxNumber = customer.TaxNumber,
            OpenAccountLimit = customer.OpenAccountLimit,
            DueDateInDays = customer.DueDateInDays,
            Discount = customer.Discount,
            PriceType = customer.PriceType,
            CustomerNote = customer.CustomerNote,
            Balance = customer.Balance
        };

        return Ok(customerDto);
    }

    // --- DEĞİŞİKLİK 2: POST (Create) Metodu "CreateCustomerDto" Kullanıyor ---
    // Sadece hızlı kayıt formundaki verileri alır.

    [HttpPost]
    public async Task<ActionResult<GetCustomerDto>> CreateCustomer(CreateCustomerDto createDto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var customer = new Customer
        {
            // CreateCustomerDto'dan gelen alanlar
            CustomerName = createDto.CustomerName,
            DueDateInDays = createDto.DueDateInDays,
            Phone = createDto.Phone,
            Address = createDto.Address,
            CustomerNote = createDto.CustomerNote,
            OpenAccountLimit = createDto.OpenAccountLimit,
            TaxOffice = createDto.TaxOffice,
            TaxNumber = createDto.TaxNumber,

            // Otomatik veya varsayılan değerler
            UserId = currentUserId,
            CustomerCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(), // Örnek müşteri kodu
            CustomerType = "Bireysel" // Varsayılan değer
        };

        // ÖNEMLİ: Artık DTO'ya özel bir validator kullanmalısın.
        // var validator = new CreateCustomerDtoValidator();
        // var validationResult = validator.Validate(createDto);
        // if (!validationResult.IsValid) { return BadRequest(validationResult.Errors); }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Oluşturulan müşterinin tüm bilgilerini (GetCustomerDto) geri dönelim.
        var resultDto = await GetCustomer(customer.Id);

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, resultDto.Value);
    }

    // --- DEĞİŞİKLİK 3: PUT (Update) Metodu "UpdateCustomerDto" Kullanıyor ---
    // Detaylı müşteri bilgi ekranındaki tüm verileri alıp günceller.

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerDto updateDto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (id != updateDto.Id)
        {
            return BadRequest("ID uyuşmuyor.");
        }

        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

        if (existingCustomer == null)
        {
            return NotFound("Müşteri bulunamadı veya yetkiniz yok.");
        }

        // Tüm alanları updateDto'dan alarak güncelle
        existingCustomer.CustomerType = updateDto.CustomerType;
        existingCustomer.CustomerName = updateDto.CustomerName;
        existingCustomer.CustomerLastName = updateDto.CustomerLastName;
        existingCustomer.Email = updateDto.Email;
        existingCustomer.Phone = updateDto.Phone;
        existingCustomer.Country = updateDto.Country;
        existingCustomer.City = updateDto.City;
        existingCustomer.District = updateDto.District;
        existingCustomer.Address = updateDto.Address;
        existingCustomer.PostalCode = updateDto.PostalCode;
        existingCustomer.TaxOffice = updateDto.TaxOffice;
        existingCustomer.TaxNumber = updateDto.TaxNumber;
        existingCustomer.OpenAccountLimit = updateDto.OpenAccountLimit;
        existingCustomer.DueDateInDays = updateDto.DueDateInDays;
        existingCustomer.Discount = updateDto.Discount;
        existingCustomer.PriceType = updateDto.PriceType;
        existingCustomer.CustomerNote = updateDto.CustomerNote;

        // ÖNEMLİ: Artık DTO'ya özel bir validator kullanmalısın.
        // var validator = new UpdateCustomerDtoValidator();
        // var validationResult = validator.Validate(updateDto);
        // if (!validationResult.IsValid) { return BadRequest(validationResult.Errors); }

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

    // --- BU METODLARDA DEĞİŞİKLİK YAPILMADI ---
    [HttpGet("{id}/detailed-summary")]
    public async Task<ActionResult<CustomerDetailedSummaryDto>> GetCustomerDetailedSummary(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

        if (customer == null)
        {
            return NotFound("Müşteri bulunamadı veya yetkiniz yok.");
        }

        // 1. Veritabanındaki ödeme tiplerini çekelim.
        var paymentTypes = await _context.PaymentTypes
            .Where(pt => pt.UserId == currentUserId)
            .ToDictionaryAsync(pt => pt.Name, pt => pt.CashRegisterType);

        // 2. Müşteriye ait tamamlanmış tüm satışları çekiyoruz.
        var allSales = await _context.Sales
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
            .Where(s => s.CustomerId == id && s.IsCompleted)
            .ToListAsync();

        // 3. Müşterinin yaptığı tüm ödemeleri (tahsilatları) çekiyoruz.
        var totalPayment = await _context.Payments
            .Where(p => p.CustomerId == id)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        // Manuel olarak eklenen tüm borçların toplamını çekiyoruz (Debt tablosu)
        var totalManualDebt = await _context.Debts
            .Where(d => d.CustomerId == id)
            .SumAsync(d => (decimal?)d.Amount) ?? 0;

        // 4. DTO'yu ve hesaplamaları hazırlıyoruz.
        var summary = new CustomerDetailedSummaryDto();

        summary.TotalSales = allSales.Sum(s => s.TotalAmount); // Satışlardan gelen brüt ciro
        summary.TotalPayment = totalPayment; // Tahsilatlardan gelen brüt ciro
        summary.RemainingBalance = customer.Balance;

        // 5. Satışları Kasa Türüne Göre Gruplama
        summary.SalesByCashRegisterType["Nakit"] = 0;
        summary.SalesByCashRegisterType["Pos"] = 0;
        summary.SalesByCashRegisterType["HariciKasa"] = 0;
        summary.SalesByCashRegisterType["Açık Hesap"] = 0;
        decimal salesDebt = 0; // Açık Hesap Satış Borcunu toplamak için geçici değişken

        foreach (var sale in allSales)
        {
            if (string.IsNullOrEmpty(sale.PaymentType)) continue;

            if (sale.PaymentType.ToLower() == "açık hesap")
            {
                summary.SalesByCashRegisterType["Açık Hesap"] += sale.TotalAmount;
                salesDebt += sale.TotalAmount;
            }
            else if (paymentTypes.TryGetValue(sale.PaymentType, out var cashRegisterType))
            {
                switch (cashRegisterType)
                {
                    case CashRegisterType.Nakit:
                        summary.SalesByCashRegisterType["Nakit"] += sale.TotalAmount;
                        break;
                    case CashRegisterType.Pos:
                        summary.SalesByCashRegisterType["Pos"] += sale.TotalAmount;
                        break;
                    case CashRegisterType.HariciKasa:
                        summary.SalesByCashRegisterType["HariciKasa"] += sale.TotalAmount;
                        break;
                }
            }
        }

        // Toplam Brüt Borcu hesaplama
        summary.TotalDebtFromSales = salesDebt + totalManualDebt;

        // YENİ KRİTİK EKLENTİ: Toplam Müşteri Geliri (Total Revenue)
        // Bu, müşteriden alınan tüm paranın (satış esnasında peşin gelen + borca karşılık sonradan gelen) toplamıdır.
        summary.TotalCustomerRevenue = summary.TotalSales + summary.TotalPayment;

        // 6. Kâr Hesaplaması (TotalProfit)
        decimal totalProfit = 0;
        foreach (var sale in allSales)
        {
            foreach (var item in sale.SaleItems)
            {
                if (item.Product != null)
                {
                    var itemProfit = (item.UnitPrice - item.Product.PurchasePrice) * item.Quantity;
                    totalProfit += itemProfit;
                }
            }
        }
        summary.TotalProfit = totalProfit;


        return Ok(summary);
    }

    [HttpGet("{id}/sales")]
    public async Task<IActionResult> GetCustomerSales(int id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new CustomerSaleListDto
            {
                SaleId = s.SaleId,
                SaleCode = s.SaleCode,
                TotalQuantity = s.TotalQuantity,
                DiscountRate = (s.SubTotalAmount > 0) ? Math.Round((s.DiscountAmount / s.SubTotalAmount * 100), 2) : 0,
                TotalAmount = s.TotalAmount,
                RemainingDebt = customer.Balance,
                PaymentType = s.PaymentType,
                PersonnelName = "Admin", // Bu alanı daha sonra dinamik yapabilirsin
                Date = s.SaleDate.ToString("dd.MM.yyyy"),
                Time = s.SaleDate.ToString("HH:mm")
            }).ToListAsync();

        return Ok(salesList);
    }
   

}