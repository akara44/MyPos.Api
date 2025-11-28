// StockTransactionReportDto.cs (Örnek)

public class StockTransactionReportDto
{
    // Raporlama Alanları (Görüntülediğiniz resimdeki başlıklarla eşleşir)
    public DateTime Date { get; set; }           // Tarih
    public string TransactionType { get; set; } // Tür (Giriş/Çıkış)
    public string? Barcode { get; set; }        // Barkod
    public string ProductName { get; set; }    // Ürün
    public string Reason { get; set; }         // Not (Reason)
    public string? CompanyNameOrCustomerName { get; set; } // Firma/Müşteri
    public string? PaymentType { get; set; }    // Ödeme Tipi
    public int Quantity { get; set; }           // Miktar
    public int BalanceAfter { get; set; }       // Kalan (Bakiye Sonrası)
    public decimal UnitPrice { get; set; }      // Birim Fiyat
    public decimal TotalAmount { get; set; }    // Tutar (Toplam Satış/Alış Tutarı)

    // Yardımcı Alanlar (Detay çekmek için kullanılır)
    public string? ReferenceType { get; set; } // Sale, PurchaseInvoice, QuickSale
    public string? ReferenceId { get; set; }   // İlgili kaydın ID'si
}