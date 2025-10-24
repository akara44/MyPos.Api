using System.Collections.Generic;

namespace MyPos.Application.Dtos.Company
{
    public class CompanyCombinedReportDto
    {
        public IEnumerable<CompanyFinancialTransactionDto> Transactions { get; set; }
        public CompanyInvoiceSummaryDto InvoiceSummary { get; set; } // Alış faturaları özeti
        public CompanyFinancialSummaryDto OverallFinancialSummary { get; set; } // Genel borç/ödeme özeti
    }

    // Alış Faturaları Özeti için yeni DTO
    public class CompanyInvoiceSummaryDto
    {
        public decimal TotalInvoiceAmount { get; set; }
        public Dictionary<string, decimal> TotalsByCashRegister { get; set; }
    }
}