using FluentValidation;
using MyPos.Application.DTOs;

namespace MyPos.Application.Validators
{
    public class CompanyValidator : AbstractValidator<CompanyDto>
    {
        public CompanyValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Firma adı boş olamaz!")
                .Length(2, 255).WithMessage("Firma adı 2-255 karakter arasında olmalıdır!");

            RuleFor(x => x.OfficialName)
                .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.OfficialName))
                .WithMessage("Yetkili adı 255 karakteri geçemez!");

            RuleFor(x => x.TermDays)
                .InclusiveBetween(0, 365).When(x => x.TermDays.HasValue)
                .WithMessage("Vade süresi 0-365 gün arasında olmalıdır!");

            RuleFor(x => x.Phone)
                .Matches(@"^[0-9\+\s]{10,20}$").When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Geçerli bir telefon numarası giriniz!");

            RuleFor(x => x.MobilePhone)
                .Matches(@"^[0-9\+\s]{10,20}$").When(x => !string.IsNullOrEmpty(x.MobilePhone))
                .WithMessage("Geçerli bir GSM numarası giriniz!");

            RuleFor(x => x.Address)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Address))
                .WithMessage("Adres 500 karakteri geçemez!");

            RuleFor(x => x.TaxNumber)
                .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.TaxNumber))
                .WithMessage("Vergi numarası 50 karakteri geçemez!");
        }
    }
}