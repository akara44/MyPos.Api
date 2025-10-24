using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ApproachingPaymentDto.cs

namespace MyPos.Application.Dtos.Company
{
    public class ApproachingPaymentDto
    {
        public DateTime PaymentDueDate { get; set; }
        public string CompanyName { get; set; }
        public decimal TotalRemainingDebt { get; set; } // Firmanın toplam kalan borcu
        public string Description { get; set; } // Elle girilen borç
        public decimal TransactionAmount { get; set; } // Bu işlemin tutarı
        public DateTime TransactionDate { get; set; } // Bu işlemin tarihi
        public int DaysUntilDue { get; set; }
    }
}