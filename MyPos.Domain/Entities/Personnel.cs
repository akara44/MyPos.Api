public class Personnel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool ViewCustomer { get; set; }
    public bool AddOrUpdateProduct { get; set; }
    public bool DeleteProduct { get; set; }
    public bool ManageCompany { get; set; }
    public bool ViewIncomeExpense { get; set; }
    public bool PurchaseInvoiceFullAccess { get; set; }
    public bool PurchaseInvoiceCreate { get; set; }
    public bool StockCount { get; set; }
    public bool SalesDiscount { get; set; }
    public bool DailyReport { get; set; }
    public bool HistoricalReport { get; set; }
    public bool ViewTurnoverInReports { get; set; }
    public decimal MaxDiscount { get; set; }
    public string DiscountCurrency { get; set; } = "TRY";
    public string IdentityNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public decimal StartingSalary { get; set; }
    public decimal CurrentSalary { get; set; }
    public string IBAN { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string UserId { get; set; }
}
