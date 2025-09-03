using MyPos.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPos.Domain.Entities
{
    public class PurchaseInvoice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public int? CompanyId { get; set; }
        public Company? Company { get; set; }

        // Yeni eklenen alanlar
        public int? PaymentTypeId { get; set; }
        public PaymentType? PaymentType { get; set; }

        public bool DoesNotAffectProfit { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal GrandTotal { get; set; }

        public ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new List<PurchaseInvoiceItem>();
    }
}