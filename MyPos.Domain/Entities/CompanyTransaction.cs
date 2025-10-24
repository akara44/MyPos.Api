using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPos.Domain.Entities
{
    public enum TransactionType
    {
        Debt, // Borç
        Payment // Ödeme
    }

    public class CompanyTransaction
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        [Required]
        public TransactionType Type { get; set; } // Borç mu Ödeme mi

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // İşlem Tutarı

        public DateTime TransactionDate { get; set; } // İşlem Tarihi

        [StringLength(500)]
        public string? Description { get; set; } // Bilgi Notu

        public int? PaymentTypeId { get; set; }
        public PaymentType? PaymentType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string UserId { get; set; }
    }
}