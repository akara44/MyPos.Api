using MyPos.Domain.Entities;

namespace MyPos.Application.Dtos
{
    public class CompanyTransactionListDto
    {
        public int Id { get; set; }
        public TransactionType Type { get; set; }
        public string TypeName => Type == TransactionType.Debt ? "Borç" : "Ödeme";
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        public string? PaymentTypeName { get; set; }
        public decimal RemainingDebt { get; set; } // İşlemden sonra kalan borç
    }
}