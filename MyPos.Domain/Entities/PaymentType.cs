using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPos.Domain.Entities
{
    public class PaymentType
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ödeme Tipi Adı boş bırakılamaz.")]
        [StringLength(100, ErrorMessage = "Ödeme Tipi Adı en fazla 100 karakter olabilir.")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Bağlı Kasa boş bırakılamaz.")]
        public CashRegisterType CashRegisterType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
        public string UserId { get; set; }
    }

    public enum CashRegisterType
    {
        Pos,
        Nakit,
        HariciKasa
    }
}