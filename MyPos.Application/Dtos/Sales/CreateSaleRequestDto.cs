using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class CreateSaleRequestDto
{
    public int? CustomerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Satış işlemi en az bir ürün içermelidir.")]
    public List<SaleItemDto> SaleItems { get; set; }

    // İndirim bilgileri (isteğe bağlı)
    public decimal? DiscountValue { get; set; }

    [RegularExpression("^(PERCENTAGE|AMOUNT)$", ErrorMessage = "İndirim tipi PERCENTAGE veya AMOUNT olmalıdır.")]
    public string? DiscountType { get; set; }

    // Muhtelif tutarlar (isteğe bağlı)
    public List<CreateMiscellaneousDto>? MiscellaneousItems { get; set; }
}
