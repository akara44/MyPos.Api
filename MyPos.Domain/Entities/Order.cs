using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Müşteri alışverişlerini temsil eden entity sınıfı.
/// </summary>
public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string OrderCode { get; set; }

    // Foreign Key: Müşteri seçimi zorunlu olmadığı için nullable yaptık.
    public int? CustomerId { get; set; }

    [Required]
    public DateTime OrderDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalDiscount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RemainingDebt { get; set; }

    [StringLength(50)]
    public string PaymentType { get; set; }

    [StringLength(500)]
    public string OrderNote { get; set; }

    // Foreign Key: Satışı yapan personel için.
    public int? PersonnelId { get; set; }

    // İlişki Tanımları
    [ForeignKey("CustomerId")]
    public Customer Customer { get; set; }
}