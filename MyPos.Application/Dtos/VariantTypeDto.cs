using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos
{
    public class CreateVariantTypeDto
    {
        [Required(ErrorMessage = "Varyant tipi adı boş olamaz.")]
        [StringLength(50, ErrorMessage = "Varyant tipi adı 50 karakterden uzun olamaz.")]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateVariantTypeDto : CreateVariantTypeDto
    {
        [Required]
        public int Id { get; set; }
    }

    public class VariantTypeResponseDto : CreateVariantTypeDto
    {
        public int Id { get; set; }
    }
}