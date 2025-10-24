using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPos.Domain.Entities
{
    public class StockTransaction
    {
        [Key]
        public int Id { get; set; }

        // Hangi ürünün stoğunun etkilendiğini belirtir
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        // Stokta yapılan değişikliğin miktarı (+ veya - olabilir)
        public int QuantityChange { get; set; }

        // İşlemin türü: "IN" (giriş) veya "OUT" (çıkış)
        [StringLength(10)]
        public string TransactionType { get; set; } = string.Empty;

        // Stok hareketinin nedeni
        [StringLength(50)]
        public string Reason { get; set; } = string.Empty; // "Sale", "Purchase", "Return", "Damage"

        public DateTime Date { get; set; }
        public int BalanceAfter { get; set; }
        public string UserId { get; set; }
    }
}