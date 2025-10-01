using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Customers
{
    public class CreateCustomerDto
    {
        // "Yeni Müşteri Oluştur" modalındaki alanlar
        public string CustomerName { get; set; } // Müşteri Tanımı
        public int DueDateInDays { get; set; }   // Vade Süresi
        public string Phone { get; set; }
        public string Address { get; set; }
        public string CustomerNote { get; set; }
        public decimal? OpenAccountLimit { get; set; } // Müşteri açık hesap limiti
        public string TaxOffice { get; set; }
        public string TaxNumber { get; set; }
    }
}