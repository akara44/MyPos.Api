using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyPos.Application.Dtos.Customers
{
    public class CustomerDetailedSummaryDto
    {
        // Ana Kartlar
        public decimal TotalSales { get; set; }        // Toplam Satış (Tüm ödeme tipleri dahil)
        public decimal TotalDebtFromSales { get; set; }  // Toplam Borç (Sadece 'Açık Hesap' satışlarından gelen)
        public decimal TotalPayment { get; set; }        // Ödeme (Müşterinin yaptığı toplam tahsilat)
        public decimal RemainingBalance { get; set; }    // Kalan Borç (Genel Bakiye)
        public decimal TotalCustomerRevenue { get; set; }

        // Toplam Satış Kartının Detayları
        public Dictionary<string, decimal> SalesByCashRegisterType { get; set; } = new Dictionary<string, decimal>();
        public decimal TotalProfit { get; set; }         // Toplam Kâr
    }
}