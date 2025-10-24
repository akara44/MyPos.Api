using MyPos.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos.Product
{
    public class UpdateCompanyTransactionDto
    {
        [Required]
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }

        [StringLength(500, ErrorMessage = "Bilgi Notu en fazla 500 karakter olabilir.")]
        public string? Description { get; set; }

        public int? PaymentTypeId { get; set; }
    }
}