using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Customer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] // Bir müşterinin mutlaka bir kullanıcıya bağlı olması gerekir.
    public string UserId { get; set; }

    [StringLength(20)]
    public string? CustomerType { get; set; } // Bireysel, Kurumsal

    [StringLength(50)]
    public string? CustomerCode { get; set; } // Müşteri Kodu

    [Required(ErrorMessage = "Müşteri Adı alanı zorunludur.")]
    [StringLength(100)]
    public string CustomerName { get; set; } // Bu zorunlu kalmalı

    [StringLength(100)]
    public string? CustomerLastName { get; set; } // Bireysel ise Soyad

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? TaxOffice { get; set; }

    [StringLength(20)]
    public string? TaxNumber { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? OpenAccountLimit { get; set; } // Veresiye Limiti

    public int DueDateInDays { get; set; } // Vade Süresi (Gün olarak)

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Discount { get; set; } // Iskonto

    public int PriceType { get; set; } // Fiyat Türü (1, 2, 3...)

    public string? CustomerNote { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Balance { get; set; } = 0;

    // İlişki Tanımları
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}