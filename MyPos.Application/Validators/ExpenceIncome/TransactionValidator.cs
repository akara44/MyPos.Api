using FluentValidation;

public class TransactionValidator : AbstractValidator<TransactionDto>
{
    public TransactionValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.TypeId).GreaterThan(0);
        RuleFor(x => x.Date).NotEmpty();

        RuleFor(x => x.PaymentType)
            .NotEmpty()
            .Must(pt => pt == "NAKİT" || pt == "POS" || pt == "KREDİ KARTI")
            .WithMessage("Invalid payment type. Must be 'NAKİT', 'POS' or 'KREDİ KARTI'.");
    }
}
