using MyPos.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SaleItem
{
    [Key]
    public int SaleItemId { get; set; }

    [ForeignKey("Sale")]
    public int SaleId { get; set; }

    [ForeignKey("Product")]
    public int ProductId { get; set; }

    public string ProductName { get; set; } // Ürün adı, hızlı raporlama için saklanabilir.

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Discount { get; set; } = 0;

    // Navigasyon Özellikleri
    public virtual Sale Sale { get; set; }
    public virtual Product Product { get; set; }
    public string UserId { get; set; }
}