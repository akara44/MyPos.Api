using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities; // CashRegisterType için
using MyPos.Infrastructure.Persistence;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Dictionary için

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
        public async Task<ActionResult<CompanyCombinedReportDto>> GetCombinedTransactions( // Dönüş tipi değişti
            int companyId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            // --- Ortak Liste Oluşturma Başlangıcı ---

            // 1. Alış faturalarını çek ve ortak DTO'ya dönüştür
            var invoices = await _context.PurchaseInvoices
                .Include(pi => pi.PaymentType)
                .Include(pi => pi.PurchaseInvoiceItems)
                .Where(pi => pi.CompanyId == companyId)
                .Select(pi => new CompanyFinancialTransactionDto
                {
                    Id = pi.Id,
                    TransactionDate = pi.InvoiceDate,
                    Description = $"Fatura No: {pi.InvoiceNumber}",
                    Type = "Alış Faturası",
                    Amount = pi.GrandTotal,
                    RemainingDebt = 0, // Geçici değer
                    PaymentTypeName = pi.PaymentType != null ? pi.PaymentType.Name : null,
                    TotalQuantity = pi.PurchaseInvoiceItems.Sum(item => item.Quantity)
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
                    RemainingDebt = 0, // Geçici değer
                    PaymentTypeName = ct.PaymentType != null ? ct.PaymentType.Name : null,
                    TotalQuantity = null
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
                if (item.Type == "Borç")
                {
                    runningBalance += item.Amount;
                }
                else if (item.Type.StartsWith("Ödeme"))
                {
                    runningBalance -= item.Amount;
                }
                item.RemainingDebt = runningBalance;
            }

            // 5. Filtrele ve en son işlemleri en üstte gösterecek şekilde sırala
            var filteredTransactions = combinedList
                .Where(t => (!startDate.HasValue || t.TransactionDate >= startDate.Value) && (!endDate.HasValue || t.TransactionDate <= endDate.Value))
                .OrderByDescending(x => x.TransactionDate)
                .ToList();

            // --- Ortak Liste Oluşturma Sonu ---


            // --- Alış Faturaları Özeti Hesaplama (İlk JSON) ---
            var purchaseInvoicesForSummary = await _context.PurchaseInvoices
                .Include(pi => pi.PaymentType)
                .Where(pi => pi.CompanyId == companyId)
                .Where(pi => (!startDate.HasValue || pi.InvoiceDate >= startDate.Value) && (!endDate.HasValue || pi.InvoiceDate <= endDate.Value)) // Filtreleri uygula
                .ToListAsync();

            var totalInvoiceAmount = purchaseInvoicesForSummary.Sum(i => i.GrandTotal);

            var totalsByCashRegister = purchaseInvoicesForSummary
                .Where(i => i.PaymentType != null) // PaymentType'ı olmayan faturaları atla
                .GroupBy(i => i.PaymentType.CashRegisterType.ToString())
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

            var invoiceSummary = new CompanyInvoiceSummaryDto
            {
                TotalInvoiceAmount = totalInvoiceAmount,
                TotalsByCashRegister = totalsByCashRegister
            };
            // --- Alış Faturaları Özeti Hesaplama Sonu ---


            // --- Genel Finansal Özet Hesaplama (İkinci JSON) ---
            // Bu özet, tarih filtrelerine uygun olarak hesaplanmalıdır.
            var filteredCompanyTransactions = await _context.CompanyTransactions
                .Where(ct => ct.CompanyId == companyId)
                .Where(ct => (!startDate.HasValue || ct.TransactionDate >= startDate.Value) && (!endDate.HasValue || ct.TransactionDate <= endDate.Value))
                .ToListAsync();

            var totalDebt = filteredCompanyTransactions
                .Where(ct => ct.Type == TransactionType.Debt)
                .Sum(ct => ct.Amount);

            var totalPayments = filteredCompanyTransactions
                .Where(ct => ct.Type == TransactionType.Payment)
                .Sum(ct => ct.Amount);

            var overallRemainingDebt = totalDebt - totalPayments; // Bu sadece filtre uygulanan işlemlerin özeti

            // Ancak gerçek kalan borç için tüm işlemleri (filtre öncesi) kullanmak daha mantıklı olabilir.
            // Eğer "gerçek" kalan borç her zaman tüm tarihçeye göre isteniyorsa, şu kullanılmalı:
            var initialTotalDebt = await _context.CompanyTransactions
                .Where(ct => ct.CompanyId == companyId && ct.Type == TransactionType.Debt)
                .SumAsync(ct => ct.Amount);
            var initialTotalPayments = await _context.CompanyTransactions
                .Where(ct => ct.CompanyId == companyId && ct.Type == TransactionType.Payment)
                .SumAsync(ct => ct.Amount);
            var actualRemainingDebt = initialTotalDebt - initialTotalPayments;

            var overallFinancialSummary = new CompanyFinancialSummaryDto
            {
                TotalDebt = totalDebt, // Filtrelenmiş borç
                TotalPayments = totalPayments, // Filtrelenmiş ödeme
                RemainingDebt = actualRemainingDebt // Tüm geçmişe göre kalan borç
            };
            // --- Genel Finansal Özet Hesaplama Sonu ---


            // Tüm verileri ana DTO'ya doldur ve geri dön
            var response = new CompanyCombinedReportDto
            {
                Transactions = filteredTransactions,
                InvoiceSummary = invoiceSummary,
                OverallFinancialSummary = overallFinancialSummary
            };

            return Ok(response);
        }
    }
}