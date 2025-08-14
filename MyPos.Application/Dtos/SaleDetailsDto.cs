using System;
using System.Collections.Generic;

public class SaleDetailsDto
{
    public int SaleId { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } // Müşteri adını da göstermek için eklendi.
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; }
    public DateTime SaleDate { get; set; }
    public bool IsCompleted { get; set; }
    public List<SaleItemDetailsDto> SaleItems { get; set; }
}