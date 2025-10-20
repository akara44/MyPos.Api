using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Reports
{
    public class GroupReportItemDto
    {
        public string GroupName { get; set; } // Grup Adı
        public decimal TotalQuantity { get; set; } // Toplam Miktar
        public decimal TotalSales { get; set; } // Toplam Satış (TotalPrice)
        public decimal TotalProfit { get; set; } // Toplam Kâr
        public int SoldProductsCount { get; set; } // Satılan Tekil Ürün Sayısı
    }

    public class GroupReportRequestDto
    {
        // Resimdeki gibi tarih ve saat parametrelerini alacak
        // Not: Tarih/saat aralığını tam olarak belirlemek için DateTime kullanılıyor.
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
      
    }
}
