using System;
using System.ComponentModel.DataAnnotations;

public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Müşteri Tanımı alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Müşteri Tanımı en fazla 100 karakter olabilir.")]
    public string CustomerName { get; set; }

    // Vade Süresi - Bu alanın veri tipi, uygulamanızın mantığına göre değişebilir (örneğin, int, string, TimeSpan).
    public string DueDate { get; set; }

    // Telefon
    public string Phone { get; set; }

    // Adres
    public string Address { get; set; }

    // Müşteri notu
    public string CustomerNote { get; set; }

    // Müşteri açık hesap limiti
    // Görüntüdeki "0" değeri varsayılan olarak alındığı için, bu alanı nullable decimal olarak tanımlamak esnekliği artırabilir.
    public decimal? OpenAccountLimit { get; set; }

    // Vergi Dairesi
    public string TaxOffice { get; set; }

    // Vergi No
    public string TaxNumber { get; set; }
}