using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Customers
{
    public class CustomerCombinedTransactionDto
    {
        public int Id { get; set; } // Hareketin kendi ID'si (SaleId, PaymentId veya DebtId)
        public DateTime TransactionDate { get; set; }
        public string TransactionTime { get; set; } // HH:mm formatında
        public string Description { get; set; } // Açıklama veya Not
        public string Type { get; set; } // "Satış", "Ödeme", "Borç"
        public decimal Amount { get; set; } // İşlemin tutarı
        public decimal RemainingBalance { get; set; } // İşlem sonrası müşterinin kalan bakiyesi
        public string PaymentTypeName { get; set; } // Ödeme tipi adı (Nakit, Pos, vb.)
        public decimal? TotalQuantity { get; set; } // Eğer satış ise toplam ürün miktarı
        public string PersonnelName { get; set; } // İşlemi yapan personel
    }
}
