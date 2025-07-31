using System.ComponentModel.DataAnnotations;

namespace MyPos.Application.DTOs
{
    public class CompanyDto
    {
        public int Id { get; set; } // Güncelleme ve okuma için

        [Required(ErrorMessage = "Firma adı zorunludur!")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Firma adı 2-255 karakter arasında olmalıdır!")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Yetkili adı 255 karakteri geçemez!")]
        public string OfficialName { get; set; } = string.Empty;

        [Range(0, 365, ErrorMessage = "Vade süresi 0-365 gün arasında olmalıdır!")]
        public int? TermDays { get; set; }

        [RegularExpression(@"^[0-9\+\s]{10,20}$", ErrorMessage = "Geçerli bir telefon numarası giriniz!")]
        public string Phone { get; set; } = string.Empty;

        [RegularExpression(@"^[0-9\+\s]{10,20}$", ErrorMessage = "Geçerli bir GSM numarası giriniz!")]
        public string MobilePhone { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Adres 500 karakteri geçemez!")]
        public string Address { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Vergi dairesi 100 karakteri geçemez!")]
        public string TaxOffice { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Vergi numarası 50 karakteri geçemez!")]
        public string TaxNumber { get; set; } = string.Empty;

        // Oluşturma tarihi (Opsiyonel)
        public DateTime? CreatedDate { get; set; }
    }
}