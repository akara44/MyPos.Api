using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using MyPos.Infrastructure.Persistence;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MyPos.Application.Dtos.Company;
using System.Security.Claims; // Bu using'i ekle
using Microsoft.AspNetCore.Authorization; // Bu using'i ekle

namespace MyPos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Auth'u aç ve tüm controller için geçerli kıl
    public class CompanyFinancialReportsController : ControllerBase
    {
        private readonly MyPosDbContext _context;

        public CompanyFinancialReportsController(MyPosDbContext context)
        {
            _context = context;
        }

        [HttpGet("CombinedList/{companyId}")]
        public async Task<ActionResult<CompanyCombinedReportDto>> GetCombinedTransactions(
            int companyId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
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
                    RemainingDebt = 0,
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
                    RemainingDebt = 0,
                    PaymentTypeName = ct.PaymentType != null ? ct.PaymentType.Name : null,
                    TotalQuantity = null
                })
                .ToListAsync();

            // ... (diğer kodlar aynı kalır)
            var combinedList = invoices
                .Concat(transactions)
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Type)
                .ToList();

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

            var filteredTransactions = combinedList
                .Where(t => (!startDate.HasValue || t.TransactionDate >= startDate.Value) && (!endDate.HasValue || t.TransactionDate <= endDate.Value))
                .OrderByDescending(x => x.TransactionDate)
                .ToList();

            var purchaseInvoicesForSummary = await _context.PurchaseInvoices
                .Include(pi => pi.PaymentType)
                .Where(pi => pi.CompanyId == companyId)
                .Where(pi => (!startDate.HasValue || pi.InvoiceDate >= startDate.Value) && (!endDate.HasValue || pi.InvoiceDate <= endDate.Value))
                .ToListAsync();

            var totalInvoiceAmount = purchaseInvoicesForSummary.Sum(i => i.GrandTotal);

            var totalsByCashRegister = purchaseInvoicesForSummary
                .Where(i => i.PaymentType != null)
                .GroupBy(i => i.PaymentType.CashRegisterType.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(i => i.GrandTotal));

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

            var initialTotalDebt = await _context.CompanyTransactions
                .Where(ct => ct.CompanyId == companyId && ct.Type == TransactionType.Debt)
                .SumAsync(ct => ct.Amount);
            var initialTotalPayments = await _context.CompanyTransactions
                .Where(ct => ct.CompanyId == companyId && ct.Type == TransactionType.Payment)
                .SumAsync(ct => ct.Amount);
            var actualRemainingDebt = initialTotalDebt - initialTotalPayments;

            var overallFinancialSummary = new CompanyFinancialSummaryDto
            {
                TotalDebt = totalDebt,
                TotalPayments = totalPayments,
                RemainingDebt = actualRemainingDebt
            };

            var response = new CompanyCombinedReportDto
            {
                Transactions = filteredTransactions,
                InvoiceSummary = invoiceSummary,
                OverallFinancialSummary = overallFinancialSummary
            };

            return Ok(response);
        }

        // ... (ApproachingPayments metodu için aşağıdaki değişiklikleri yap)
        [HttpGet("ApproachingPayments")]
        public async Task<ActionResult<IEnumerable<ApproachingPaymentDto>>> GetApproachingPayments()
        {
            // JWT'den mevcut kullanıcının ID'sini al
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. ADIM: Kullanıcının tüm firmalarını al
            var userCompanies = await _context.Company
                .Where(c => c.UserId == currentUserId && c.TermDays.HasValue && c.TermDays.Value > 0)
                .ToListAsync();

            var approachingPayments = new List<ApproachingPaymentDto>();

            foreach (var company in userCompanies)
            {
                // 2. ADIM: Her firma için toplam borç/ödeme durumunu hesapla
                var totalDebt = await _context.CompanyTransactions
                    .Where(ct => ct.CompanyId == company.Id && ct.Type == TransactionType.Debt)
                    .SumAsync(ct => ct.Amount);

                var totalPayments = await _context.CompanyTransactions
                    .Where(ct => ct.CompanyId == company.Id && ct.Type == TransactionType.Payment)
                    .SumAsync(ct => ct.Amount);

                var remainingDebt = totalDebt - totalPayments;

                // 3. ADIM: Eğer kalan borç varsa, vade kontrolü yap
                if (remainingDebt > 0)
                {
                    // Son borç işlemini al (vade hesabı için)
                    var lastDebtTransaction = await _context.CompanyTransactions
                        .Where(ct => ct.CompanyId == company.Id && ct.Type == TransactionType.Debt)
                        .OrderByDescending(ct => ct.TransactionDate)
                        .FirstOrDefaultAsync();

                    if (lastDebtTransaction != null)
                    {
                        // 4. ADIM: Vade tarihini hesapla
                        var paymentDueDate = lastDebtTransaction.TransactionDate.AddDays(company.TermDays.Value);
                        var daysUntilDue = (int)(paymentDueDate.Date - DateTime.Today).TotalDays;

                        // 5. ADIM: 30 gün içinde ödenmesi gerekiyorsa listeye ekle
                        if (daysUntilDue <= 30)
                        {
                            approachingPayments.Add(new ApproachingPaymentDto
                            {
                                PaymentDueDate = paymentDueDate,
                                CompanyName = company.Name,
                                TotalRemainingDebt = remainingDebt, // Firmanın toplam kalan borcu
                                Description = lastDebtTransaction.Description ?? "Son borç işlemi",
                                TransactionAmount = lastDebtTransaction.Amount, // Son işlemin tutarı
                                TransactionDate = lastDebtTransaction.TransactionDate, // Son işlemin tarihi
                                DaysUntilDue = daysUntilDue
                            });
                        }
                    }
                }
            }

            return Ok(approachingPayments.OrderBy(p => p.PaymentDueDate));
        }
    }
}