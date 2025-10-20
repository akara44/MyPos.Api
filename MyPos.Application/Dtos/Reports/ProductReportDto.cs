using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Reports
{
    public class ProductSaleReportDto
    {
        public string ProductBarcode { get; set; }
        public string ProductName { get; set; }
        public decimal TotalQuantitySold { get; set; } // Satış Miktarı
        public int RemainingStock { get; set; }        // Kalan Stok (Product tablosundan)
        public decimal AvgPurchasePrice { get; set; }  // Ort. Birim Alış
        public decimal AvgSalePrice { get; set; }      // Ort. Birim Satış Fiyatı
        public decimal AvgUnitProfit { get; set; }     // Ort. Birim Kar
        public decimal TotalAmount { get; set; }       // Toplam Tutar (Satış)
        public decimal TotalProfitLoss { get; set; }   // Toplam Kar/Zarar
    }

    // İsteğe bağlı tarih aralığı için bir DTO
    public class ProductReportRequestDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
