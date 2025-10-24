using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Reports
{
    // Bu DTO'ları SaleController'daki DTO'ların yanına veya ayrı bir 'Dtos' klasörüne ekleyin.

    public class ProductBarcodeDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public decimal SalePrice { get; set; }
    }

    public class ProductCorrelationItemDto
    {
        public string ProductBarcode { get; set; }
        public string ProductName { get; set; }
        public int TotalSaleQuantity { get; set; } // Bu barkodun, ana ürünle birlikte toplam satış adedi (tekil satış sayacı değil)
        public decimal TotalAmount { get; set; } // Bu ürünün toplam tutarı (TotalSaleQuantity * UnitPrice)
        public decimal TotalProfit { get; set; } // Bu ürünün toplam karı
    }

    public class ProductCorrelationReportDto
    {
        public string MainProductBarcode { get; set; }
        public string MainProductName { get; set; }
        // Rapor ekranınızdaki tablo formatına uyması için bir liste döndürüyoruz
        public List<ProductCorrelationItemDto> CorrelatedProducts { get; set; }
    }
}
