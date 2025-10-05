using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Debt
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } // Hangi kullanıcıya ait olduğu

    [Required]
    public int CustomerId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; } // Eklenen borç miktarı

    [Required]
    public DateTime DebtDate { get; set; } // Borç eklenme tarihi

    [StringLength(500)]
    public string? Note { get; set; } // Borçla ilgili not

    public int? SaleId { get; set; } // Eğer bu borç bir satıştan kaynaklanıyorsa (Açık Hesap gibi)

    // Navigation Property
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }
}