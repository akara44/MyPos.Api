// UpdateProductWithImageDtoValidator.cs (Bu dosyayı MyPos.Application.Validators klasöründe oluşturmanız gerekiyor)
using FluentValidation;
using Microsoft.AspNetCore.Http;
using MyPos.Application.Dtos;

namespace MyPos.Application.Validators
{
    public class UpdateProductWithImageDtoValidator : AbstractValidator<UpdateProductWithImageDto>
    {
        public UpdateProductWithImageDtoValidator()
        {
            RuleFor(x => x.Barcode)
                .NotEmpty().WithMessage("Barkod boş olamaz.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ürün adı boş olamaz.")
                .MaximumLength(100).WithMessage("Ürün adı en fazla 100 karakter olabilir.");

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("Stok miktarı negatif olamaz.");

            RuleFor(x => x.CriticalStockLevel)
                .GreaterThanOrEqualTo(0).WithMessage("Kritik stok seviyesi negatif olamaz.");

            RuleFor(x => x.SalePrice)
                .GreaterThan(0).WithMessage("Satış fiyatı 0'dan büyük olmalıdır.");

            RuleFor(x => x.PurchasePrice)
                .GreaterThan(0).WithMessage("Alış fiyatı 0'dan büyük olmalıdır.");

            RuleFor(x => x.TaxRate)
                .InclusiveBetween(0, 100).WithMessage("Vergi oranı 0 ile 100 arasında olmalıdır.");

            // ProductGroupId için kural ekledik
            RuleFor(x => x.ProductGroupId)
                .GreaterThan(0).WithMessage("Geçerli bir Ürün Grup ID'si seçilmelidir.");

            RuleFor(x => x.SalePageList)
                .NotEmpty().WithMessage("Satış sayfa listesi boş olamaz.")
                .MaximumLength(50).WithMessage("Satış sayfa listesi en fazla 50 karakter olabilir.");

            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("Birim boş olamaz.")
                .MaximumLength(20).WithMessage("Birim en fazla 20 karakter olabilir.");

            RuleFor(x => x.OriginCountry)
                .NotEmpty().WithMessage("Menşei ülke boş olamaz.")
                .MaximumLength(50).WithMessage("Menşei ülke en fazla 50 karakter olabilir.");

            // Resim dosyası için doğrulama (isteğe bağlı ve sadece dosya varsa)
            RuleFor(x => x.ImageFile)
                .Must(file => file == null || IsValidImageFile(file))
                .WithMessage("Sadece .jpg, .jpeg veya .png dosyaları yüklenebilir.")
                .When(x => x.ImageFile != null);
        }

        private bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return true; // Boş dosya için kontrol ProductsController'da yapılıyor

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png";
        }
    }
}