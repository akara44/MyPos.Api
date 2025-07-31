// ProductVariantDtos.cs
using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos
{
    public class CreateProductVariantDto
    {
        [Required(ErrorMessage = "Ürün ID boş olamaz.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Barkod boş olamaz.")]
        [StringLength(50, ErrorMessage = "Barkod 50 karakterden uzun olamaz.")]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Varyant adı boş olamaz.")]
        [StringLength(255, ErrorMessage = "Varyant adı 255 karakterden uzun olamaz.")]
        public string Name { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Stok değeri negatif olamaz.")]
        public int Stock { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "Kritik stok seviyesi negatif olamaz.")]
        public int CriticalStockLevel { get; set; } = 10;

        [Range(0.01, double.MaxValue, ErrorMessage = "Satış fiyatı sıfırdan büyük olmalıdır.")]
        public decimal SalePrice { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Alış fiyatı sıfırdan büyük olmalıdır.")]
        public decimal PurchasePrice { get; set; }

        [Range(0, 100, ErrorMessage = "Vergi oranı 0 ile 100 arasında olmalıdır.")]
        public int TaxRate { get; set; } = 18;

        [StringLength(50, ErrorMessage = "Satış sayfa listesi 50 karakterden uzun olamaz.")]
        public string SalePageList { get; set; } = "Liste1";

        // Bu varyanta ait varyant değerlerinin ID'leri (örn: [1, 5] -> Renk: Mavi, Beden: L)
        [Required(ErrorMessage = "Varyant değerleri boş olamaz.")]
        public List<int> VariantValueIds { get; set; } = new List<int>();
    }

    public class UpdateProductVariantDto : CreateProductVariantDto
    {
        [Required(ErrorMessage = "ID boş olamaz.")]
        public int Id { get; set; }
    }

    public class ProductVariantResponseDto : CreateProductVariantDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty; // Ana ürün adı
        public decimal ProfitRate { get; set; }
        public string? ImageUrl { get; set; } // Resim URL'si

        // Varyant değerlerini ve tiplerini içeren bir liste
        public List<VariantDetailDto> VariantDetails { get; set; } = new List<VariantDetailDto>();
    }

    // ProductVariantResponseDto içinde kullanılacak yardımcı DTO
    public class VariantDetailDto
    {
        public int VariantTypeId { get; set; }
        public string VariantTypeName { get; set; } = string.Empty;
        public int VariantValueId { get; set; }
        public string VariantValueName { get; set; } = string.Empty;
    }
}