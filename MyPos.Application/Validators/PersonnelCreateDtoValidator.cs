using FluentValidation;

public class PersonnelCreateDtoValidator : AbstractValidator<PersonnelCreateDto>
{
    public PersonnelCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Password).MinimumLength(5);
        RuleFor(x => x.IdentityNumber)
            .Length(11)
            .When(x => !string.IsNullOrEmpty(x.IdentityNumber));
    }
}
