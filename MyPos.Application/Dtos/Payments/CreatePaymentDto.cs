using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos.Payments
{
    public class CreatePaymentDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ödeme tutarı 0'dan büyük olmalıdır.")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        // DEĞİŞİKLİK: string yerine PaymentTypeId (int) kullanılıyor
        [Required(ErrorMessage = "Ödeme Tipi seçimi zorunludur.")]
        public int PaymentTypeId { get; set; } // YENİ ALAN

        public string? Note { get; set; }
    }
}