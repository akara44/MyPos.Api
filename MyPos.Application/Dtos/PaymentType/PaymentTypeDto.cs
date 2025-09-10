using System.ComponentModel.DataAnnotations;
using MyPos.Domain.Entities;

namespace MyPos.Application.Dtos.PaymentType
{
    public class PaymentTypeDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ödeme Tipi Adı boş bırakılamaz.")]
        [StringLength(100, ErrorMessage = "Ödeme Tipi Adı en fazla 100 karakter olabilir.")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Bağlı Kasa boş bırakılamaz.")]
        public CashRegisterType CashRegisterType { get; set; }
    }
}