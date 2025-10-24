using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Customers
{
    public class CustomerApproachingPaymentDto
    {
        public DateTime DueDate { get; set; } // Alacak Tarihi (Vade Tarihi)
        public string DueDateDescription { get; set; } // "Yarın", "10 Gün Sonra" gibi açıklama
        public string CustomerName { get; set; } // Müşteri Adı
        public decimal RemainingBalance { get; set; } // Kalan Borç
        public string SalesCode { get; set; } // Borcun Kaynağı (Satış Kodu veya "Manuel Borç")
        public decimal TransactionAmount { get; set; } // İşlemin Tutarı (Satış/Borç Tutarı)
        public DateTime TransactionDate { get; set; } // İşlem Tarihi (Satış/Borç Tarihi)
        public int DaysUntilDue { get; set; } // Kalan Gün Sayısı
    }
}
