// VariantValue.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPos.Domain.Entities
{
    public class VariantValue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Value { get; set; } = string.Empty;

        // Hangi VariantType'a ait olduğunu belirten Foreign Key
        public int VariantTypeId { get; set; }

        [ForeignKey("VariantTypeId")]
        public VariantType? VariantType { get; set; }
    }
}