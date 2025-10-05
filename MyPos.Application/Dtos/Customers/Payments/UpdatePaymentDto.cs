using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos.Customers.Payments
{
    // Bir ödemeyi güncellerken alınacak veriler.
    public class UpdatePaymentDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ödeme tutarı 0'dan büyük olmalıdır.")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required(ErrorMessage = "Ödeme Tipi seçimi zorunludur.")]
        public int PaymentTypeId { get; set; }

        public string? Note { get; set; }
    }
}