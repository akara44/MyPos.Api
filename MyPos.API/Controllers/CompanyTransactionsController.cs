using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos.Company;
using MyPos.Application.Dtos.Product;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Linq;
using System.Security.Claims; // User ID'yi almak için eklendi
using System.Threading.Tasks;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Auth'u aç ve tüm controller için geçerli kıl
    public class CompanyTransactionsController : ControllerBase
    {
        private readonly MyPosDbContext _context;

        public CompanyTransactionsController(MyPosDbContext context)
        {
            _context = context;
        }

        [HttpPost("Add")]
        public async Task<IActionResult> AddCompanyTransaction([FromBody] CreateCompanyTransactionDto transactionDto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Firmanın mevcut kullanıcıya ait olduğunu doğrula
            var company = await _context.Company
                                         .FirstOrDefaultAsync(c => c.Id == transactionDto.CompanyId && c.UserId == currentUserId);

            if (company == null)
            {
                return Unauthorized("Bu firmaya işlem yapma yetkiniz yok.");
            }

            if (transactionDto.Type == TransactionType.Payment && !transactionDto.PaymentTypeId.HasValue)
            {
                ModelState.AddModelError(nameof(transactionDto.PaymentTypeId), "Ödeme işlemleri için Ödeme Tipi seçilmelidir.");
                return BadRequest(ModelState);
            }

            var transaction = new CompanyTransaction
            {
                CompanyId = transactionDto.CompanyId,
                Type = transactionDto.Type,
                Amount = transactionDto.Amount,
                TransactionDate = transactionDto.TransactionDate,
                Description = transactionDto.Description,
                PaymentTypeId = transactionDto.PaymentTypeId,
                CreatedDate = DateTime.Now,
                UserId = currentUserId
            };

            _context.CompanyTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return StatusCode(201, "İşlem başarıyla eklendi.");
        }

        [HttpGet("List/{companyId}")]
        public async Task<ActionResult<IEnumerable<CompanyTransactionListDto>>> GetCompanyTransactions(
          int companyId,
          [FromQuery] DateTime? startDate,
          [FromQuery] DateTime? endDate)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Firmanın mevcut kullanıcıya ait olduğunu doğrula
            var company = await _context.Company
                                         .FirstOrDefaultAsync(c => c.Id == companyId && c.UserId == currentUserId);

            if (company == null)
            {
                return Unauthorized("Bu firmaya ait işlemleri görme yetkiniz yok.");
            }

            var transactions = await _context.CompanyTransactions
              .Include(ct => ct.PaymentType)
              .Where(ct => ct.CompanyId == companyId)
              .OrderBy(ct => ct.TransactionDate)
              .ToListAsync();

            var runningBalance = 0m;
            var transactionList = new List<CompanyTransactionListDto>();

            foreach (var t in transactions)
            {
                if (t.Type == TransactionType.Debt)
                {
                    runningBalance += t.Amount;
                }
                else if (t.Type == TransactionType.Payment)
                {
                    runningBalance -= t.Amount;
                }

                if ((!startDate.HasValue || t.TransactionDate >= startDate.Value) && (!endDate.HasValue || t.TransactionDate <= endDate.Value))
                {
                    transactionList.Add(new CompanyTransactionListDto
                    {
                        Id = t.Id,
                        Type = t.Type,
                        Amount = t.Amount,
                        TransactionDate = t.TransactionDate,
                        Description = t.Description,
                        PaymentTypeName = t.PaymentType?.Name,
                        RemainingDebt = runningBalance
                    });
                }
            }

            return Ok(transactionList.OrderByDescending(t => t.TransactionDate));
        }

        [HttpGet("Summary/{companyId}")]
        public async Task<ActionResult<CompanyFinancialSummaryDto>> GetCompanyFinancialSummary(int companyId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Firmanın mevcut kullanıcıya ait olduğunu doğrula
            var company = await _context.Company
                                         .FirstOrDefaultAsync(c => c.Id == companyId && c.UserId == currentUserId);

            if (company == null)
            {
                return Unauthorized("Bu firmaya ait özet raporu görme yetkiniz yok.");
            }

            var totalDebt = await _context.CompanyTransactions
              .Where(ct => ct.CompanyId == companyId && ct.Type == TransactionType.Debt)
              .SumAsync(ct => ct.Amount);

            var totalPayments = await _context.CompanyTransactions
              .Where(ct => ct.CompanyId == companyId && ct.Type == TransactionType.Payment)
              .SumAsync(ct => ct.Amount);

            var remainingDebt = totalDebt - totalPayments;

            var summary = new CompanyFinancialSummaryDto
            {
                TotalDebt = totalDebt,
                TotalPayments = totalPayments,
                RemainingDebt = remainingDebt
            };

            return Ok(summary);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompanyTransaction(int id, [FromBody] UpdateCompanyTransactionDto transactionDto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id != transactionDto.Id)
            {
                return BadRequest();
            }

            // İşlem yapılacak veriyi bulurken hem ID'yi hem de User ID'yi kontrol et
            var transactionToUpdate = await _context.CompanyTransactions
                                                    .Include(t => t.Company)
                                                    .FirstOrDefaultAsync(t => t.Id == id && t.Company.UserId == currentUserId);

            if (transactionToUpdate == null)
            {
                return Unauthorized("Bu işlemi güncelleme yetkiniz yok.");
            }

            if (transactionDto.Type == TransactionType.Payment && !transactionDto.PaymentTypeId.HasValue)
            {
                ModelState.AddModelError(nameof(transactionDto.PaymentTypeId), "Ödeme işlemleri için Ödeme Tipi seçilmelidir.");
                return BadRequest(ModelState);
            }

            transactionToUpdate.CompanyId = transactionDto.CompanyId;
            transactionToUpdate.Type = transactionDto.Type;
            transactionToUpdate.Amount = transactionDto.Amount;
            transactionToUpdate.TransactionDate = transactionDto.TransactionDate;
            transactionToUpdate.Description = transactionDto.Description;
            transactionToUpdate.PaymentTypeId = transactionDto.PaymentTypeId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CompanyTransactionExists(id, currentUserId)) // UserId kontrolü ile güncellendi
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
        public async Task<IActionResult> DeleteCompanyTransaction(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // İşlemi bulurken hem ID'yi hem de User ID'yi kontrol et
            var transaction = await _context.CompanyTransactions
                                            .Include(t => t.Company)
                                            .FirstOrDefaultAsync(t => t.Id == id && t.Company.UserId == currentUserId);

            if (transaction == null)
            {
                return Unauthorized("Bu işlemi silme yetkiniz yok.");
            }

            _context.CompanyTransactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // UserId kontrolü eklenmiş yeni metot
        private bool CompanyTransactionExists(int id, string userId)
        {
            return _context.CompanyTransactions.Any(e => e.Id == id && e.Company.UserId == userId);
        }
    }
}