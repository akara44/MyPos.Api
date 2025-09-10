public class StockAdjustmentDto
{
    public int ProductId { get; set; }
    public int QuantityChange { get; set; } 
    public string Reason { get; set; } = string.Empty; 
}