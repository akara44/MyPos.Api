public class StockAdjustmentDto
{
    public int ProductId { get; set; }
    public int QuantityChange { get; set; } // Pozitif veya negatif olabilir
    public string Reason { get; set; } = string.Empty; // "Satış", "Alış", "İade" gibi
}