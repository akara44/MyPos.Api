using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Customers
{
    public class CreateCustomerDto
    {
        public string CustomerName { get; set; } // Ad veya Firma Ünvanı
        public string? CustomerLastName { get; set; } // Soyad
        public string? Email { get; set; } // E-posta
        public string? CustomerType { get; set; } // Bireysel/Kurumsal seçimi için
        public int DueDateInDays { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string CustomerNote { get; set; }
        public decimal? OpenAccountLimit { get; set; }
        public string TaxOffice { get; set; }
        public string TaxNumber { get; set; }
    }
}