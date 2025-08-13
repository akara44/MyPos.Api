using System;
using System.ComponentModel.DataAnnotations;


public class OrderListDto
{
    public int Id { get; set; }
    public string OrderCode { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal RemainingDebt { get; set; }
    public string PaymentType { get; set; }
    public string PersonnelName { get; set; } // Satışı yapan personelin adını taşır.
}