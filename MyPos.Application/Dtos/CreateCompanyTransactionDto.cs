using MyPos.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos
{
    public class CreateCompanyTransactionDto
    {
        public int CompanyId { get; set; }
        public TransactionType Type { get; set; } // Borç veya Ödeme
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [StringLength(500, ErrorMessage = "Bilgi Notu en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        public int? PaymentTypeId { get; set; }
    }
}