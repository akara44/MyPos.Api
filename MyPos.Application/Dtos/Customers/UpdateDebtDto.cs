using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos.Customers.Debts
{
    public class UpdateDebtDto
    {
        [Required]
        public int Id { get; set; } // Güncellenecek borç kaydının ID'si

        [Required]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Borç Tutarı alanı zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Borç tutarı 0'dan büyük olmalıdır.")]
        public decimal Amount { get; set; } // Yeni borç miktarı

        [Required]
        public DateTime DebtDate { get; set; }

        public string? Note { get; set; }
    }
}