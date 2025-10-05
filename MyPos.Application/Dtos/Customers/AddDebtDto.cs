using System;
using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos.Customers.Debts
{
    public class CreateDebtDto
    {
        [Required]
        public int CustomerId { get; set; } // Hangi müşteriye borç ekleneceği

        [Required(ErrorMessage = "Borç Tutarı alanı zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Borç tutarı 0'dan büyük olmalıdır.")]
        public decimal Amount { get; set; } // Eklenecek borç miktarı

        [Required]
        public DateTime DebtDate { get; set; } // Borcun hangi tarihte eklendiği

        // Borcun hangi 'türü' olduğunu belirtebiliriz (Örn: 'Veresiye', 'İade Borcu' vb. - Şimdilik basit tutalım)
        // [Required]
        // public int DebtTypeId { get; set; } // İsteğe bağlı, PaymentType gibi bir DebtType tablosu gerektirir.

        public string? Note { get; set; } // Borçla ilgili not
    }
}