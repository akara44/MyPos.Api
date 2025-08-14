using System.ComponentModel.DataAnnotations;

public class SaleItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Miktar 0'dan büyük olmalıdır.")]
    public int Quantity { get; set; }

    // Not: UnitPrice ve Discount gibi alanlar, back-end'de stoktan çekilebilir
    // veya front-end'den de gönderilebilir. Bu örnekte, back-end'in
    // stoktan çekmesi daha güvenli bir yaklaşımdır, bu yüzden şimdilik
    // DTO'da opsiyonel olarak bırakıldı.
}