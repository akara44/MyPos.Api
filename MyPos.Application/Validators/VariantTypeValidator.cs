using FluentValidation;
using MyPos.Application.Dtos;

namespace MyPos.Application.Validators
{
    public class CreateVariantTypeDtoValidator : AbstractValidator<CreateVariantTypeDto>
    {
        public CreateVariantTypeDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Varyant tipi adı boş olamaz.")
                .MaximumLength(50).WithMessage("Varyant tipi adı 50 karakterden uzun olamaz.");
        }
    }

    public class UpdateVariantTypeDtoValidator : AbstractValidator<UpdateVariantTypeDto>
    {
        public UpdateVariantTypeDtoValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Geçerli bir ID sağlanmalıdır.");
            Include(new CreateVariantTypeDtoValidator()); // Create validator kurallarını da dahil edin
        }
    }
}