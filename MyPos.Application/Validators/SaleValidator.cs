using System.Linq;

public class SaleValidator
{
    public ValidationResult Validate(CreateSaleRequestDto request)
    {
        var result = new ValidationResult();

        // 1. Satış kalemleri listesinin boş olup olmadığını kontrol et.
        if (request.SaleItems == null || !request.SaleItems.Any())
        {
            result.Errors.Add("Satış işlemi en az bir ürün içermelidir.");
        }
        else
        {
            // 2. Her bir satış kalemini döngüye al ve doğrula.
            for (int i = 0; i < request.SaleItems.Count; i++)
            {
                var item = request.SaleItems[i];
                if (item.ProductId <= 0)
                {
                    result.Errors.Add($"Ürün {i + 1} için geçerli bir ProductId belirtilmelidir.");
                }

                if (item.Quantity <= 0)
                {
                    result.Errors.Add($"Ürün {i + 1} için miktar 0'dan büyük olmalıdır.");
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