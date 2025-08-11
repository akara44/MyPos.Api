using FluentValidation;
using MyPos.Application.Dtos;

namespace MyPos.Application.Validators
{
    public class CreatePurchaseInvoiceDtoValidator : AbstractValidator<CreatePurchaseInvoiceDto>
    {
        public CreatePurchaseInvoiceDtoValidator()
        {
            RuleFor(x => x.InvoiceNumber)
                .NotEmpty().WithMessage("Fatura numarası boş olamaz.")
                .MaximumLength(50).WithMessage("Fatura numarası 50 karakteri geçemez.");

            RuleFor(x => x.InvoiceDate)
                .NotEmpty().WithMessage("Fatura tarihi boş olamaz.");

            RuleFor(x => x.CompanyId)
                .NotNull().WithMessage("Firma seçimi zorunludur.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Faturada en az bir kalem olmalıdır.");

            RuleForEach(x => x.Items).SetValidator(new PurchaseInvoiceItemDtoValidator());
        }
    }

    public class PurchaseInvoiceItemDtoValidator : AbstractValidator<PurchaseInvoiceItemDto>
    {
        public PurchaseInvoiceItemDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotNull().WithMessage("Ürün seçimi zorunludur.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Birim fiyat sıfırdan küçük olamaz.");
        }
    }
}