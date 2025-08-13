using FluentValidation;
using MyPos.Application.Dtos;

namespace MyPos.Application.Validators
{
    public class UpdatePurchaseInvoiceDtoValidator : AbstractValidator<UpdatePurchaseInvoiceDto>
    {
        public UpdatePurchaseInvoiceDtoValidator()
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
}