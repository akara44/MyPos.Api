using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } // Ödemeyi alan kullanıcının kimliği

    [Required]
    public int CustomerId { get; set; } // Ödemeyi yapan müşteri

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; } // Ödeme tutarı

    [Required]
    public DateTime PaymentDate { get; set; } // Ödeme tarihi

    // PaymentType SADECE BİR KERE TANIMLANDI
    [Required]
    [StringLength(50)]
    public string PaymentType { get; set; } // Nakit, Kredi Kartı vb.

    [StringLength(500)]
    public string? Note { get; set; } // Ödeme ile ilgili not

    // Bir ödeme her zaman bir satışa bağlı olmayabilir.
    // Örneğin müşteri sadece hesabındaki borcu kapatıyor olabilir.
    // Bu yüzden nullable (?) yapmak daha doğru bir yaklaşımdır.
    public int? SaleId { get; set; }

    // Navigation Property
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }
}