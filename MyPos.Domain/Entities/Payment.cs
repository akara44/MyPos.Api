using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Müşteri ödemelerini temsil eden entity sınıfı.
/// </summary>
public class Payment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Foreign Key: Müşteri ile ilişki.
    [Required]
    public int CustomerId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    // İlişki Tanımları
    [ForeignKey("CustomerId")]
    public Customer Customer { get; set; }
}