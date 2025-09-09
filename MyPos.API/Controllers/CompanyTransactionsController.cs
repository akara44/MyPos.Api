using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            // Tüm işlemleri tarihe göre sıralı bir şekilde alıyoruz
            var transactions = await _context.CompanyTransactions
                .Include(ct => ct.PaymentType)
                .Where(ct => ct.CompanyId == companyId)
                .OrderBy(ct => ct.TransactionDate)
                .ToListAsync();

            var currentBalance = 0m;
            var transactionList = new List<CompanyTransactionListDto>();

            // Tüm borç ve ödemeleri toplayarak mevcut borcu hesapla
            foreach (var t in transactions)
            {
                if (t.Type == TransactionType.Debt)
                {
                    currentBalance += t.Amount;
                }
                else if (t.Type == TransactionType.Payment)
                {
                    currentBalance -= t.Amount;
                }
            }

            // Listeyi tersine çevirerek en son işlemi en üstte göster ve her işlemden sonraki kalan borcu hesapla
            foreach (var t in transactions.OrderByDescending(x => x.TransactionDate).ToList())
            {
                var remainingDebt = currentBalance;

                // Her bir işlemin kalan borcunu hesapla
                // Bu işlem şu anki kalan borçtan çıkarılan/eklenen işlem olduğu için ters mantıkta ilerliyoruz.
                if (t.Type == TransactionType.Debt)
                {
                    currentBalance -= t.Amount;
                }
                else if (t.Type == TransactionType.Payment)
                {
                    currentBalance += t.Amount;
                }

                if ((startDate.HasValue && t.TransactionDate < startDate.Value) || (endDate.HasValue && t.TransactionDate > endDate.Value))
                {
                    continue; // Filtre dışı kalanları atla
                }

                transactionList.Add(new CompanyTransactionListDto
                {
                    Id = t.Id,
                    Type = t.Type,
                    Amount = t.Amount,
                    TransactionDate = t.TransactionDate,
                    Description = t.Description,
                    PaymentTypeName = t.PaymentType?.Name,
                    RemainingDebt = remainingDebt
                });
            }

            return Ok(transactionList);
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