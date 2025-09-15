using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


public class Payment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SaleId { get; set; }   // EKLENDİ

    [Required]
    public int CustomerId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    [Required]
    public string PaymentTypeName { get; set; } // EKLENDİ

    [ForeignKey("SaleId")]
    public Sale Sale { get; set; }

    [ForeignKey("CustomerId")]
    public Customer Customer { get; set; }
    public string UserId { get; set; }
}
