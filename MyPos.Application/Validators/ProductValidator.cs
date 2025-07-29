using FluentValidation;
using MyPos.Application.Dtos;

namespace MyPos.Application.Validators

{
    public class ProductValidator : AbstractValidator<CreateProductDto>
    {
        public ProductValidator()
        {
            RuleFor(x => x.Barcode).NotEmpty().Length(1, 50);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SalePrice).GreaterThan(0);
            RuleFor(x => x.PurchasePrice).GreaterThan(0);
            RuleFor(x => x.TaxRate).InclusiveBetween(0, 100); // KDV 0-100 arası
            RuleFor(x => x.Unit).NotEmpty().When(x => !string.IsNullOrEmpty(x.Unit));
        }
    }
}