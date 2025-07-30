// VariantValueValidator.cs
using FluentValidation;
using MyPos.Application.Dtos;

namespace MyPos.Application.Validators
{
    public class CreateVariantValueDtoValidator : AbstractValidator<CreateVariantValueDto>
    {
        public CreateVariantValueDtoValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Varyant değeri boş olamaz.")
                .MaximumLength(100).WithMessage("Varyant değeri 100 karakterden uzun olamaz.");

            RuleFor(x => x.VariantTypeId)
                .GreaterThan(0).WithMessage("Geçerli bir Varyant Tipi ID'si sağlanmalıdır.");
        }
    }

    public class UpdateVariantValueDtoValidator : AbstractValidator<UpdateVariantValueDto>
    {
        public UpdateVariantValueDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Geçerli bir ID sağlanmalıdır.");
            Include(new CreateVariantValueDtoValidator()); // Create validator kurallarını da dahil edin
        }
    }
}