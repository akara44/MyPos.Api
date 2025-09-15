using System;
using System.ComponentModel.DataAnnotations;

namespace MyPos.Domain.Entities
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty; 

        [StringLength(255)]
        public string OfficialName { get; set; } = string.Empty;

        public int? TermDays { get; set; }

        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(20)]
        public string MobilePhone { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string TaxOffice { get; set; } = string.Empty;

        [StringLength(50)]
        public string TaxNumber { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public string UserId { get; set; }
    }
}