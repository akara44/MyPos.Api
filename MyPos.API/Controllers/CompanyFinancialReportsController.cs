using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Linq;

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyFinancialReportsController : ControllerBase
    {
        private readonly MyPosDbContext _context;

        public CompanyFinancialReportsController(MyPosDbContext context)
        {
            _context = context;
        }

        [HttpGet("CombinedList/{companyId}")]
        public async Task<ActionResult<IEnumerable<CompanyFinancialTransactionDto>>> GetCombinedTransactions(
            int companyId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            // 1. Alış faturalarını çek ve ortak DTO'ya dönüştür
            var invoices = await _context.PurchaseInvoices
                .Where(pi => pi.CompanyId == companyId)
                .Select(pi => new CompanyFinancialTransactionDto
                {
                    Id = pi.Id,
                    TransactionDate = pi.InvoiceDate,
                    Description = $"Fatura No: {pi.InvoiceNumber}",
                    Type = "Alış Faturası",
                    Amount = pi.GrandTotal,
                    RemainingDebt = 0
                })
                .ToListAsync();

            // 2. Borç/ödeme hareketlerini çek ve ortak DTO'ya dönüştür
            var transactions = await _context.CompanyTransactions
                .Include(ct => ct.PaymentType)
                .Where(ct => ct.CompanyId == companyId)
                .Select(ct => new CompanyFinancialTransactionDto
                {
                    Id = ct.Id,
                    TransactionDate = ct.TransactionDate,
                    Description = ct.Description ?? (ct.Type == TransactionType.Payment ? "Ödeme" : "Borç"),
                    Type = ct.Type == TransactionType.Debt ? "Borç" : $"Ödeme - {ct.PaymentType.Name}",
                    Amount = ct.Amount,
                    RemainingDebt = 0
                })
                .ToListAsync();

            // 3. İki listeyi birleştir ve tarihe göre sırala
            var combinedList = invoices
                .Concat(transactions)
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Type)
                .ToList();

            // 4. Her işlemden sonraki kalan borcu hesapla
            var runningBalance = 0m;
            foreach (var item in combinedList)
            {
                if (item.Type == "Alış Faturası" || item.Type == "Borç")
                {
                    runningBalance += item.Amount;
                }
                else
                {
                    runningBalance -= item.Amount;
                }

                item.RemainingDebt = runningBalance;
            }

            // 5. Filtrele ve en son işlemleri en üstte gösterecek şekilde sırala
            var filteredList = combinedList
                .Where(t => (!startDate.HasValue || t.TransactionDate >= startDate.Value) && (!endDate.HasValue || t.TransactionDate <= endDate.Value))
                .OrderByDescending(x => x.TransactionDate)
                .ToList();

            return Ok(filteredList);
        }
    }
}