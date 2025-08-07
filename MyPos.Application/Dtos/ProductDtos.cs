// ProductDto.cs
using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.Dtos
{
    public class CreateProductDto
    {
        [Required]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public int Stock { get; set; } = 0;
        public int CriticalStockLevel { get; set; } = 10; // Varsayılan değer
        public decimal SalePrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public int TaxRate { get; set; } = 18; // Varsayılan KDV
        public string SalePageList { get; set; } = "Liste1";
        public int? ProductGroupId { get; set; }    
        public string Unit { get; set; } = "Adet";
        public string OriginCountry { get; set; } = "Türkiye";
        // ImageUrl'ü CreateProductDto'ya da eklemek isterseniz buraya ekleyebilirsiniz,
        // ancak genellikle resim yükleme ayrı bir adım olduğu için sadece Response DTO'da bulunur.
        // public string? ImageUrl { get; set; } // Eğer ürün oluşturulurken URL sağlanacaksa
    }

    public class ProductResponseDto : CreateProductDto
    {
        public int Id { get; set; }
        public decimal ProfitRate { get; set; }
        public string ProductGroupName { get; set; } = string.Empty;
        
        public string? ImageUrl { get; set; }
    }
}