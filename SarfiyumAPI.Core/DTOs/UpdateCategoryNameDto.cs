using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class UpdateCategoryNameDto
    {
        [Required(ErrorMessage = "Kategori adı boş olamaz.")]
        [MaxLength(250, ErrorMessage = "Kategori adı en fazla 250 karakter olabilir.")]
        public string Name { get; set; } = string.Empty;
    }
}
