using System.ComponentModel.DataAnnotations;

namespace SistemaVotoModelos
{
    public class Candidato
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(10, MinimumLength = 10)]
        [RegularExpression(@"^\d{10}$")]
        public string Cedula { get; set; } = string.Empty;
        public Votante? Votante { get; set; }
        [Required]
        public int ListaId { get; set; }
        public Lista? Lista { get; set; }
        [Required]
        public int EleccionId { get; set; }
        public Eleccion? Eleccion { get; set; }
        [Required]
        public string RolPostulante { get; set; } = string.Empty;
    }
}
