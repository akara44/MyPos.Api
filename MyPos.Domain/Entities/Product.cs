// Product.cs (Güncellenmiş Kısım)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPos.Domain.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int Stock { get; set; } 
        public int CriticalStockLevel { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [NotMapped]
        public decimal ProfitRate => (SalePrice - PurchasePrice) / PurchasePrice * 100;

        public int TaxRate { get; set; }

        [StringLength(50)]
        public string SalePageList { get; set; } = "Liste1";

        public int? ProductGroupId { get; set; }
        public ProductGroup? ProductGroup { get; set; }

        [StringLength(20)]
        public string Unit { get; set; } = "Adet";

        [StringLength(50)]
        public string OriginCountry { get; set; } = "Türkiye";

        public string? ImageUrl { get; set; }

        // Yeni eklenen: Bu ürüne ait varyantlar
        public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    }
}