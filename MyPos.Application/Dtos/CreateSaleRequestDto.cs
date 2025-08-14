using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class CreateSaleRequestDto
{
    // Müşteri ID'si isteğe bağlıdır, bu yüzden nullable bırakıldı.
    public int? CustomerId { get; set; }

    // Bir satış işlemi birden fazla kalem içerebilir.
    [Required]
    [MinLength(1, ErrorMessage = "Satış işlemi en az bir ürün içermelidir.")]
    public List<SaleItemDto> SaleItems { get; set; }
}