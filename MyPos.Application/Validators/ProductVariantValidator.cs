using FluentValidation;
using MyPos.Application.Dtos;

namespace MyPos.Application.Validators
{
    public class CreateProductVariantDtoValidator : AbstractValidator<CreateProductVariantDto>
    {
        public CreateProductVariantDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("Geçerli bir Ürün ID'si sağlanmalıdır.");

            RuleFor(x => x.Barcode)
                .NotEmpty().WithMessage("Barkod boş olamaz.")
                .Length(1, 50).WithMessage("Barkod 1 ile 50 karakter arasında olmalıdır.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Varyant adı boş olamaz.")
                .MaximumLength(255).WithMessage("Varyant adı 255 karakterden uzun olamaz.");

            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("Stok değeri negatif olamaz.");
            RuleFor(x => x.CriticalStockLevel).GreaterThanOrEqualTo(0).WithMessage("Kritik stok seviyesi negatif olamaz.");
            RuleFor(x => x.SalePrice).GreaterThan(0).WithMessage("Satış fiyatı sıfırdan büyük olmalıdır.");
            RuleFor(x => x.PurchasePrice).GreaterThan(0).WithMessage("Alış fiyatı sıfırdan büyük olmalıdır.");
            RuleFor(x => x.TaxRate).InclusiveBetween(0, 100).WithMessage("Vergi oranı 0 ile 100 arasında olmalıdır.");

            RuleFor(x => x.VariantValueIds)
                .NotNull().WithMessage("Varyant değerleri listesi boş olamaz.")
                .Must(list => list != null && list.Any()).WithMessage("En az bir varyant değeri seçilmelidir.");
        }
    }

    public class UpdateProductVariantDtoValidator : AbstractValidator<UpdateProductVariantDto>
    {
        public UpdateProductVariantDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Geçerli bir ID sağlanmalıdır.");
            Include(new CreateProductVariantDtoValidator()); // Create validator kurallarını da dahil edin
        }
    }
}