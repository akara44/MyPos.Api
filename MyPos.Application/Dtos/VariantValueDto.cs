using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos
{
    public class CreateVariantValueDto
    {
        [Required(ErrorMessage = "Varyant değeri boş olamaz.")]
        [StringLength(100, ErrorMessage = "Varyant değeri 100 karakterden uzun olamaz.")]
        public string Value { get; set; } = string.Empty;

        [Required(ErrorMessage = "Varyant tipi ID'si boş olamaz.")]
        public int VariantTypeId { get; set; }
    }

    public class UpdateVariantValueDto : CreateVariantValueDto
    {
        [Required]
        public int Id { get; set; }
    }

    public class VariantValueResponseDto : CreateVariantValueDto
    {
        public int Id { get; set; }
        public string VariantTypeName { get; set; } = string.Empty; // Response için varyant tipi adını da ekleyelim
    }
}