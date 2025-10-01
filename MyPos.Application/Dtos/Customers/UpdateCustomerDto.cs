using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Customers
{
    public class UpdateCustomerDto
    {
        public int Id { get; set; } // Güncelleme için ID gerekli
        public string CustomerType { get; set; }
        public string CustomerName { get; set; }
        public string CustomerLastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string TaxOffice { get; set; }
        public string TaxNumber { get; set; }
        public decimal? OpenAccountLimit { get; set; }
        public int DueDateInDays { get; set; }
        public decimal Discount { get; set; }
        public int PriceType { get; set; }
        public string CustomerNote { get; set; }
    }
}
