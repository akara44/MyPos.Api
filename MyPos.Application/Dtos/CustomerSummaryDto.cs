using System;

/// <summary>
/// Müşteri detay sayfasının üst kısmındaki finansal özet bilgilerini taşır.
/// </summary>
public class CustomerSummaryDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal RemainingBalance { get; set; }
}