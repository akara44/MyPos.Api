using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Application.Dtos.Customers
{
    public class CustomerDto
    {
        public int Id { get; set; } 
        public string CustomerName { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string CustomerNote { get; set; } = string.Empty;
        public decimal? OpenAccountLimit { get; set; }
        public string TaxOffice { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
    }
}
