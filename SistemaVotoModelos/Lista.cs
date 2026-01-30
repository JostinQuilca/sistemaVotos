using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaVotoModelos
{
    public class Lista
    {
        [Key]
        public int Id { get; set; }
        public string NombreLista { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public int EleccionId { get; set; }
    }
}