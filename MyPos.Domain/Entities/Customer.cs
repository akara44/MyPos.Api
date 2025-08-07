// Models/Customer.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

/// <summary>
/// Müşteri verilerini temsil eden entity sınıfı.
/// </summary>
public class Customer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "Müşteri Tanımı alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Müşteri Tanımı en fazla 100 karakter olmalıdır.")]
    public string CustomerName { get; set; }

    public string DueDate { get; set; }

    
    public string Phone { get; set; }

    public string Address { get; set; }
    public string CustomerNote { get; set; }
    public decimal? OpenAccountLimit { get; set; }
    public string TaxOffice { get; set; }
    public string TaxNumber { get; set; }

    // İlişki Tanımları
    // Bir müşterinin birden fazla siparişi olabilir.
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    // Bir müşterinin birden fazla ödemesi olabilir.
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}