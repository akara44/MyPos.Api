using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos.Customers
{
    public class AddDebtDto
    {
        [Required]
        public int CustomerId { get; set; } // Hangi müşteriye borç ekleneceği

        [Required(ErrorMessage = "Veresiye Borç Tutarı alanı zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Borç tutarı sıfırdan büyük olmalıdır.")]
        public decimal DebtAmount { get; set; } // Eklenecek borç miktarı (Veresiye Tutar)

        public string? DebtNote { get; set; } // Borçla ilgili not (Veresiye Notu)
    }
}