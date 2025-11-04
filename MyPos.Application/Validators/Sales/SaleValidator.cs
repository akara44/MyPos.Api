using System.Linq;

public class SaleValidator
{
    public ValidationResult Validate(CreateSaleRequestDto request)
    {
        var result = new ValidationResult();

        // 1. Satış kalemleri listesi kontrolü (Mevcut hali)
        if (request.SaleItems == null || !request.SaleItems.Any())
        {
            result.Errors.Add("Satış işlemi en az bir ürün içermelidir.");
        }
        else
        {
            // 2. Her bir satış kalemini döngüye al ve doğrula (Mevcut hali)
            for (int i = 0; i < request.SaleItems.Count; i++)
            {
                var item = request.SaleItems[i];
                if (item.ProductId <= 0)
                {
                    result.Errors.Add($"Ürün {i + 1} için geçerli bir ProductId belirtilmelidir.");
                }

                if (item.Quantity <= 0) // Quantity için [Range(1, int.MaxValue)] DTO'da var, tekrar kontrolü iyi.
                {
                    result.Errors.Add($"Ürün {i + 1} için miktar 0'dan büyük olmalıdır.");
                }
            }
        }

        // --- YENİ EKLENEN KONTROLLER ---

        // 3. İskonto Kontrolü: Null değilse (>0 zorunluluğu)
        if (request.DiscountValue.HasValue)
        {
            if (string.IsNullOrEmpty(request.DiscountType))
            {
                result.Errors.Add("İndirim değeri girildiğinde indirim tipi de belirtilmelidir.");
            }
            if (request.DiscountValue <= 0)
            {
                result.Errors.Add("İndirim değeri 0'dan büyük olmalıdır.");
            }
            if (request.DiscountType == "PERCENTAGE" && request.DiscountValue > 100)
            {
                result.Errors.Add("Yüzde indirimi 100'den fazla olamaz.");
            }
        }

        // 4. Muhtelif Tutar Kontrolü: Null değilse (>0 zorunluluğu)
        if (request.MiscellaneousItems != null)
        {
            for (int i = 0; i < request.MiscellaneousItems.Count; i++)
            {
                var miscItem = request.MiscellaneousItems[i];
                // Bu kontrol CreateMiscellaneousDto'da [Range] ile sağlanıyor, ancak ekstra güvenlik için ekleyebiliriz.
                // Not: Description'ın boş olmaması DTO'daki [Required] ile sağlanacaktır.

                if (string.IsNullOrEmpty(miscItem.Description))
                {
                    result.Errors.Add($"Muhtelif Tutar {i + 1} için açıklama zorunludur.");
                }

                if (miscItem.Amount <= 0)
                {
                    result.Errors.Add($"Muhtelif Tutar {i + 1} için tutar 0'dan büyük olmalıdır.");
                }
            }
        }   
        // 3. Daha ileri seviye doğrulama kuralları buraya eklenebilir.
        // Örneğin:
        // - Müşteri ID'si veritabanında mevcut mu?
        // - Her bir ProductId veritabanında mevcut mu?
        // - Her bir ürünün stok miktarı, istenen miktarı karşılıyor mu? (Bu genellikle servis katmanında yapılır)

        return result;
    }
}

public class ValidationResult
{
    public List<string> Errors { get; set; } = new List<string>();

    public bool IsValid => !Errors.Any();
}