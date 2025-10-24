using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// PaymentType entity'si için gerekli namespace'i ekleyin
using MyPos.Domain.Entities;

public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    // DEĞİŞİKLİK: String PaymentType alanı KALDIRILDI, yerine ID eklendi
    [Required]
    public int PaymentTypeId { get; set; } // YENİ FOREIGN KEY ALANI

    [StringLength(500)]
    public string? Note { get; set; }

    public int? SaleId { get; set; }

    // Navigation Property
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }

    // YENİ NAVİGASYON ÖZELLİĞİ: Foreign Key ilişkisini tanımlar
    [ForeignKey("PaymentTypeId")]
    public virtual PaymentType PaymentType { get; set; }
}