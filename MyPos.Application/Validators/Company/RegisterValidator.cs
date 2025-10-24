using FluentValidation;
using MyPos.Application.Dtos.Auth;

namespace MyPos.Application.Validators.Company;
        
public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MinimumLength(2);
        RuleFor(x => x.LastName).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty().MinimumLength(10);
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .Matches("[A-Z]").WithMessage("Parola en az bir büyük harf içermeli.")
            .Matches("[a-z]").WithMessage("Parola en az bir küçük harf içermeli.")
            .Matches("[0-9]").WithMessage("Parola en az bir rakam içermeli.");
    }
}
