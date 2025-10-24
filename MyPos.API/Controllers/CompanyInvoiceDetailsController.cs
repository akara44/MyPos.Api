using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.PurchaseInvoice;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization; // Bu using'i ekle
using System.Security.Claims; // Bu using'i ekle
using System.Linq;
using System.Threading.Tasks;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Auth'u aç ve tüm controller için geçerli kıl
    public class InvoiceReportsController : ControllerBase
    {
        private readonly MyPosDbContext _context;

        public InvoiceReportsController(MyPosDbContext context)
        {
            _context = context;
        }

        [HttpGet("CompanyInvoices/{companyId}")]
        public async Task<IActionResult> GetCompanyInvoicesReport(int companyId, DateTime? startDate, DateTime? endDate)
        {
            // JWT'den mevcut kullanıcının ID'sini al
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kullanıcının, istediği firmaya erişim yetkisi olup olmadığını kontrol et
            var company = await _context.Company
                                        .FirstOrDefaultAsync(c => c.Id == companyId && c.UserId == currentUserId);

            if (company == null)
            {
                return NotFound("Firma bulunamadı veya bu firmaya erişim yetkiniz yok.");
            }

            var query = _context.PurchaseInvoices
                .Include(x => x.Company)
                .Include(x => x.PaymentType)
                .Where(x => x.CompanyId == companyId);

            if (startDate.HasValue)
                query = query.Where(x => x.InvoiceDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.InvoiceDate <= endDate.Value);

            var invoices = await query
                .Select(x => new PurchaseInvoiceListDto
                {
                    Id = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    InvoiceDate = x.InvoiceDate,
                    CompanyName = x.Company.Name,
                    GrandTotal = x.GrandTotal,
                    PaymentTypeName = x.PaymentType != null ? x.PaymentType.Name : string.Empty,
                    PaymentTypeCashRegister = x.PaymentType != null ? x.PaymentType.CashRegisterType.ToString() : string.Empty,
                    TotalQuantity = x.PurchaseInvoiceItems.Sum(i => i.Quantity)
                })
                .ToListAsync();

            var totalAmount = invoices.Sum(i => i.GrandTotal);

            // Kasa türüne göre gruplama
            var totalsByCashRegister = invoices
                .GroupBy(i => i.PaymentTypeCashRegister)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.GrandTotal));

            // Tüm kasa türlerini ekle, eksik olanları 0 olarak ata
            var allCashRegisters = Enum.GetValues(typeof(CashRegisterType))
                .Cast<CashRegisterType>()
                .Select(c => c.ToString());

            foreach (var cashRegister in allCashRegisters)
            {
                if (!totalsByCashRegister.ContainsKey(cashRegister))
                    totalsByCashRegister[cashRegister] = 0;
            }

            var response = new
            {
                TotalAmount = totalAmount,
                TotalsByCashRegister = totalsByCashRegister,
                Invoices = invoices
            };

            return Ok(response);
        }
    }
}