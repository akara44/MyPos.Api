namespace MyPos.Application.Dtos
{
    // Alış faturası oluşturma ve güncelleme için kullanılan DTO
    public class CreatePurchaseInvoiceDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public int? CompanyId { get; set; }
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

    // Alış faturası listeleme veya detayları için kullanılan DTO
    public class PurchaseInvoiceDetailsDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal GrandTotal { get; set; }
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
    public class UpdatePurchaseInvoiceDto
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public int? CompanyId { get; set; }
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new List<PurchaseInvoiceItemDto>();
    }
    public class PurchaseInvoiceListDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
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
}