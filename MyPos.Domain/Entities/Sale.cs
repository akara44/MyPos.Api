using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Sale
{
    [Key]
    public int SaleId { get; set; }

    [ForeignKey("Customer")]
    public int? CustomerId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SubTotalAmount { get; set; } // Ürünlerin toplam tutarı

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiscountAmount { get; set; } = 0; // İndirim tutarı

    public string? DiscountType { get; set; } // "PERCENTAGE" veya "AMOUNT"

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiscountValue { get; set; } = 0; // Yüzde değeri veya tutar değeri

    [Column(TypeName = "decimal(18, 2)")]
    public decimal MiscellaneousTotal { get; set; } = 0; // Muhtelif tutarların toplamı

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; } // Nihai tutar (SubTotal - Discount + Miscellaneous)

    public string? PaymentType { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public bool IsCompleted { get; set; } = false;
    public bool IsDiscountApplied { get; set; } = false; // İndirim uygulandı mı kontrolü

    // Navigasyon Özellikleri
    public virtual Customer Customer { get; set; }
    public virtual ICollection<SaleItem> SaleItems { get; set; }
    public virtual ICollection<SaleMiscellaneous> SaleMiscellaneous { get; set; }
}