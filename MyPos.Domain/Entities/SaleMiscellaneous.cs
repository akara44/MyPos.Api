using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SaleMiscellaneous
{
    [Key]
    public int SaleMiscellaneousId { get; set; }

    [ForeignKey("Sale")]
    public int SaleId { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } // "Kurye ücreti", "Ambalaj", "Servis bedeli" vs.

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigasyon Özelliği
    public virtual Sale Sale { get; set; }
    public string UserId { get; set; }
}