using FluentValidation;

public class ExpenseIncomeTypeValidator : AbstractValidator<ExpenseIncomeTypeDto>
{
    public ExpenseIncomeTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must be at most 100 characters.");
    }
}
