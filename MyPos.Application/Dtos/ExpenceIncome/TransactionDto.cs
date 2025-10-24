public class TransactionDto
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string PaymentType { get; set; } 
    public int TypeId { get; set; }
    public DateTime Date { get; set; }
}
