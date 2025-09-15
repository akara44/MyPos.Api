using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPos.Domain.Entities
{
    public class ProductVariantValue
    {
        [Key]
        public int Id { get; set; }

        // Hangi ProductVariant'a ait olduğunu belirten Foreign Key
        public int ProductVariantId { get; set; }
        [ForeignKey("ProductVariantId")]
        public ProductVariant ProductVariant { get; set; } = null!; // Zorunlu ilişki

        // Hangi VariantValue'ya ait olduğunu belirten Foreign Key
        public int VariantValueId { get; set; }
        [ForeignKey("VariantValueId")]
        public VariantValue VariantValue { get; set; } = null!; // Zorunlu ilişki
        public string UserId { get; set; }
    }
}