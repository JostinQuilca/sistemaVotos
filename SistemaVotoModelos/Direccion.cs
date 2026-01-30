using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaVotoModelos
{
    public class Direccion
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Provincia { get; set; } = string.Empty;
        [Required]
        public string Canton { get; set; } = string.Empty;
        [Required]
        public string Parroquia { get; set; } = string.Empty;
    }
}