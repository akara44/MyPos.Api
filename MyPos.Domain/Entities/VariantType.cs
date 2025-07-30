// VariantType.cs
using System.ComponentModel.DataAnnotations;

namespace MyPos.Domain.Entities
{
    public class VariantType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        // Bu varyant tipine ait değerler
        public ICollection<VariantValue>? VariantValues { get; set; }
    }
}