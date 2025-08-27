using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public class CreateMiscellaneousDto
    {
        [Required]
        [StringLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir.")]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır.")]
        public decimal Amount { get; set; }
    }

