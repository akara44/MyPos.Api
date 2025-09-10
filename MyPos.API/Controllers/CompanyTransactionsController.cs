using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Linq; // Added for .ToList() and .Concat()
using System.Threading.Tasks; // Added for async/await

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]  
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
                CreatedDate = DateTime.Now
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
            // Yalnızca Borç ve Ödeme işlemlerini tarihe göre artan sıralı (eski tarihten yeniye) şekilde alıyoruz
            var transactions = await _context.CompanyTransactions
                .Include(ct => ct.PaymentType)
                .Where(ct => ct.CompanyId == companyId)
                .OrderBy(ct => ct.TransactionDate)
                .ToListAsync();

            var runningBalance = 0m;
            var transactionList = new List<CompanyTransactionListDto>();

            // İşlemleri sırayla işleyerek her bir işlemden sonraki kalan borcu hesapla
            foreach (var t in transactions)
            {
                // Sadece manuel borç ve ödeme işlemlerini kalan borç hesaplamasına dahil et
                if (t.Type == TransactionType.Debt)
                {
                    runningBalance += t.Amount;
                }
                else if (t.Type == TransactionType.Payment)
                {
                    runningBalance -= t.Amount;
                }

                // Sadece filtreleme kriterlerine uyanları listeye ekle
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
                        RemainingDebt = runningBalance // Bu işlemden sonraki kalan borç (sadece manuel işlemlerle hesaplanmış)
                    });
                }
            }

            // En son yapılan işlemleri en üstte göstermek için listeyi tersine çevir
            return Ok(transactionList.OrderByDescending(t => t.TransactionDate));
        }

        [HttpGet("Summary/{companyId}")]
        public async Task<ActionResult<CompanyFinancialSummaryDto>> GetCompanyFinancialSummary(int companyId)
        {
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

        // PUT: api/CompanyTransactions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompanyTransaction(int id, [FromBody] UpdateCompanyTransactionDto transactionDto)
        {
            if (id != transactionDto.Id)
            {
                return BadRequest();
            }

            var transactionToUpdate = await _context.CompanyTransactions.FindAsync(id);

            if (transactionToUpdate == null)
            {
                return NotFound();
            }

            if (transactionDto.Type == TransactionType.Payment && !transactionDto.PaymentTypeId.HasValue)
            {
                ModelState.AddModelError(nameof(transactionDto.PaymentTypeId), "Ödeme işlemleri için Ödeme Tipi seçilmelidir.");
                return BadRequest(ModelState);
            }

            // DTO'daki verilerle entity'yi güncelle
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
                if (!CompanyTransactionExists(id))
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

        // DELETE: api/CompanyTransactions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompanyTransaction(int id)
        {
            var transaction = await _context.CompanyTransactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            _context.CompanyTransactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CompanyTransactionExists(int id)
        {
            return _context.CompanyTransactions.Any(e => e.Id == id);
        }
    }
}