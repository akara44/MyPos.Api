using MyPos.Domain.Entities;

namespace MyPos.Application.Dtos
{
    public class CompanyFinancialTransactionDto
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; }
        public string Type { get; set; } // "Alış Faturası", "Borç", "Ödeme"
        public decimal Amount { get; set; }
        public decimal RemainingDebt { get; set; } // İşlemden sonra kalan borç
        public string? PaymentTypeName { get; set; } // Ödeme Tipi
        public decimal? TotalQuantity { get; set; }
    }
}