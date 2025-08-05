using FluentValidation;

public class StockAdjustmentValidator : AbstractValidator<StockAdjustmentDto>
{
    public StockAdjustmentValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0)
            .WithMessage("Product ID must be greater than zero.");

        RuleFor(x => x.QuantityChange).NotEqual(0)
            .WithMessage("Stock change quantity cannot be zero.");

        RuleFor(x => x.Reason).NotEmpty()
            .WithMessage("Reason for stock adjustment is required.");
    }
}