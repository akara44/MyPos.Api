using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPos.Domain.Entities
{
    public class UserSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Hangi kullanıcıya ait?

        // Aradığın ayar: Stok yetersizse satışı engelle mi?
        // Varsayılan: false (Yani stok olmasa da satar, eksiye düşer)
        public bool BlockSaleIfNoStock { get; set; } = false;
    }
}
