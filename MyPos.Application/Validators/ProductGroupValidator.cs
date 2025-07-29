using FluentValidation;
using MyPos.Application.Dtos;

namespace MyPos.Application.Validators;

public class ProductGroupValidator : AbstractValidator<ProductGroupDto>
{
    public ProductGroupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün grup adı boş olamaz.")
            .MaximumLength(50).WithMessage("Ürün grup adı 50 karakteri geçemez.");
    }
}
