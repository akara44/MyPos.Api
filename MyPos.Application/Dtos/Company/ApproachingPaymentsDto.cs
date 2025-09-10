using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Company
{
    public class ApproachingPaymentDto
    {
        public DateTime PaymentDueDate { get; set; }
        public string CompanyName { get; set; }
        public decimal RemainingDebt { get; set; }
        public int DaysUntilDue { get; set; } // Eklenen yeni alan

    }
}
