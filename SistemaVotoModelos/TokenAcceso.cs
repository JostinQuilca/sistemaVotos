using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaVotoModelos
{
    public class TokenAcceso
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(6)]
        public string Codigo { get; set; } = string.Empty;
        // FK hacia Votante
        public string VotanteId { get; set; } = string.Empty;
        public bool EsValido { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public Votante? Votante { get; set; }
    }
}
