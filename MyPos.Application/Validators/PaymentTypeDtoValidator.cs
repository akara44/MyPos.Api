using FluentValidation;
using MyPos.Application.Dtos;



namespace MyPos.Application.Validators
{
    public class PaymentTypeDtoValidator : AbstractValidator<PaymentTypeDto>
    {
        public PaymentTypeDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ödeme Tipi Adı boş bırakılamaz.")
                .Length(1, 100).WithMessage("Ödeme Tipi Adı en fazla 100 karakter olabilir.");

            RuleFor(x => x.CashRegisterType)
                .IsInEnum().WithMessage("Geçersiz Bağlı Kasa değeri.");
        }
    }
}