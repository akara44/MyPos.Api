using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Sale
{
    [Key]
    public int SaleId { get; set; }

    [ForeignKey("Customer")]
    public int? CustomerId { get; set; } // Müşteri seçilmediyse null olabilir.

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    public string? PaymentType { get; set; } // Enum kullanmak daha iyi bir yaklaşımdır.

    public DateTime SaleDate { get; set; } = DateTime.Now;

    public bool IsCompleted { get; set; } = false;

    // Navigasyon Özellikleri
    public virtual Customer Customer { get; set; }
    public virtual ICollection<SaleItem> SaleItems { get; set; }
}