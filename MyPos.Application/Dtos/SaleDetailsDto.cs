using System;
using System.Collections.Generic;

public class SaleDetailsDto
{
    public int SaleId { get; set; }
    public string SaleCode { get; set; }
    public int TotalQuantity { get; set; } 
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal SubTotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MiscellaneousTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentType { get; set; }
    public DateTime SaleDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDiscountApplied { get; set; }
    public List<SaleItemDetailsDto> SaleItems { get; set; }
    public List<SaleMiscellaneousDto> SaleMiscellaneous { get; set; }
}