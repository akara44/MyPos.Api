using FluentValidation;
using System.Text.RegularExpressions;

public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        // Müşteri Tanımı alanı için doğrulama kuralları.
        // Gerekli, boş olamaz ve maksimum 100 karakter olmalıdır.
        RuleFor(customer => customer.CustomerName)
            .NotEmpty().WithMessage("Müşteri Tanımı boş bırakılamaz.")
            .NotNull().WithMessage("Müşteri Tanımı boş bırakılamaz.")
            .MaximumLength(100).WithMessage("Müşteri Tanımı en fazla 100 karakter olmalıdır.");

        // Telefon numarası alanı için doğrulama kuralları.
        // Opsiyonel olarak, buraya telefon formatı kontrolü de eklenebilir.
        RuleFor(customer => customer.Phone)
            .MaximumLength(20).WithMessage("Telefon numarası en fazla 20 karakter olmalıdır.");
        // Örnek: .Matches(new Regex(@"^\+?\d{10,15}$")).WithMessage("Geçerli bir telefon numarası giriniz.");

        // Adres alanı için doğrulama kuralları.
        RuleFor(customer => customer.Address)
            .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olmalıdır.");

        // Müşteri notu için doğrulama kuralları.
        RuleFor(customer => customer.CustomerNote)
            .MaximumLength(1000).WithMessage("Müşteri Notu en fazla 1000 karakter olmalıdır.");

        // Açık hesap limiti için doğrulama kuralları.
        // Negatif bir değer olamayacağını kontrol eder.
        RuleFor(customer => customer.OpenAccountLimit)
            .GreaterThanOrEqualTo(0).When(customer => customer.OpenAccountLimit.HasValue)
            .WithMessage("Açık Hesap Limiti negatif bir değer olamaz.");

        // Vergi Dairesi için doğrulama kuralları.
        RuleFor(customer => customer.TaxOffice)
            .MaximumLength(100).WithMessage("Vergi Dairesi en fazla 100 karakter olmalıdır.");

        // Vergi Numarası için doğrulama kuralları.
        RuleFor(customer => customer.TaxNumber)
            .MaximumLength(50).WithMessage("Vergi Numarası en fazla 50 karakter olmalıdır.");
        // Örnek: .Matches(new Regex(@"^\d{10}$")).WithMessage("Vergi numarası 10 haneli olmalıdır.");
    }
}
