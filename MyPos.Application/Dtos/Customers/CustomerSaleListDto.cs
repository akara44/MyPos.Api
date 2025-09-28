using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Customers
{
    public class CustomerSaleListDto
    {
        public int SaleId { get; set; }
        public string SaleCode { get; set; }         // Satış Kodu
        public int TotalQuantity { get; set; }       // Toplam Ürün
        public decimal DiscountRate { get; set; }    // İskonto Oranı (%)
        public decimal TotalAmount { get; set; }     // Toplam Tutar
        public decimal RemainingDebt { get; set; }   // Kalan Borç
        public string PaymentType { get; set; }      // Ödeme Tipi
        public string PersonnelName { get; set; }    // Personel
        public string Date { get; set; }             // Tarih (GG.AA.YYYY)
        public string Time { get; set; }             // Saat (SS:DD)
    }
}
