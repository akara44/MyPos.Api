using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace MyPos.Application.Dtos.Payments
{
    public class PaymentListDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; }
        public string Date { get; set; } // GG.AA.YYYY formatında
        public string Time { get; set; } // SS:DD formatında
        public string? Note { get; set; }
    }
}