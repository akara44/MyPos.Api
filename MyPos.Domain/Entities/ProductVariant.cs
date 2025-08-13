using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPos.Domain.Entities
{
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        // Hangi ana ürüne ait olduğunu belirten Foreign Key
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!; // Zorunlu ilişki

        [Required]
        [StringLength(50)]
        public string Barcode { get; set; } = string.Empty; // Her varyantın kendi barkodu olabilir

        [Required]
        [StringLength(255)] // Varyantın tam adını veya kombinasyonunu saklayabilir
        public string Name { get; set; } = string.Empty;

        public int Stock { get; set; }
        public int CriticalStockLevel { get; set; } = 10;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [NotMapped]
        public decimal ProfitRate => (SalePrice - PurchasePrice) / PurchasePrice * 100;

        public int TaxRate { get; set; } = 18;

        [StringLength(50)]
        public string SalePageList { get; set; } = "Liste1";

        public string? ImageUrl { get; set; } // Varyanta özel resim de olabilir

        // Bu varyantın sahip olduğu varyant değerleri (Renk: Mavi, Beden: L gibi)
        public ICollection<ProductVariantValue> ProductVariantValues { get; set; } = new List<ProductVariantValue>();
    }
}