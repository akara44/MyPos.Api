public class Income
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }

  
    public string PaymentType { get; set; }

    public DateTime Date { get; set; }

    // Bağlantı
    public int TypeId { get; set; }
    public ExpenseIncomeType Type { get; set; }
}
