using MyPos.Domain.Entities;

namespace MyPos.Application.Dtos
{
    // Alış faturası oluşturma için kullanılan DTO
    public class CreatePurchaseInvoiceDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public int? CompanyId { get; set; }
        public int PaymentTypeId { get; set; }
        public bool DoesNotAffectProfit { get; set; }
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new List<PurchaseInvoiceItemDto>();
    }

    // Fatura kalemi için DTO
    public class PurchaseInvoiceItemDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int TaxRate { get; set; }
        public decimal? DiscountRate1 { get; set; }
        public decimal? DiscountRate2 { get; set; }
    }

    // Alış faturası güncelleme için kullanılan DTO
    public class UpdatePurchaseInvoiceDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public int? CompanyId { get; set; }
        public int PaymentTypeId { get; set; }
        public bool DoesNotAffectProfit { get; set; }
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new List<PurchaseInvoiceItemDto>();
    }

    // Alış faturası detayları için kullanılan DTO
    public class PurchaseInvoiceDetailsDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string PaymentTypeName { get; set; } = string.Empty;
        public bool DoesNotAffectProfit { get; set; }
        public List<PurchaseInvoiceItemDetailsDto> Items { get; set; } = new List<PurchaseInvoiceItemDetailsDto>();
    }

    // Fatura kalemi detayları için kullanılan DTO
    public class PurchaseInvoiceItemDetailsDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int TaxRate { get; set; }
        public decimal? DiscountRate1 { get; set; }
        public decimal? DiscountRate2 { get; set; }
    }

    // Alış faturası listeleme için kullanılan DTO
    public class PurchaseInvoiceListDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public string PaymentTypeName { get; set; }
        public string PaymentTypeCashRegister { get; set; }
    }

    // Fatura listeleme ve filtreleme için kullanılan DTO
    public class PurchaseInvoiceFilterDto
    {
        public string? InvoiceNumber { get; set; }
        public int? CompanyId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }

    // Listeleme işlemi için sonuç DTO'su (toplam sayıyı da dönebilmek için)
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    // Ödeme Tipi DTO'su
    public class PaymentTypesDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public CashRegisterType CashRegisterType { get; set; }
    }
    public class CompanyInvoicesResponseDto
    {
        public decimal TotalAmount { get; set; } // Toplam alışveriş
        public List<PurchaseInvoiceListDto> Invoices { get; set; }
    }
}