using System.ComponentModel.DataAnnotations;

namespace SistemaVotoModelos
{
    public class Votante
    {
        [Key]
        [Required]
        [StringLength(10, MinimumLength = 10)]
        [RegularExpression(@"^\d{10}$")]
        public string Cedula { get; set; } = string.Empty;
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        [RegularExpression(@"^[^@\s]+@(utn\.edu\.ec|gmail\.com|outlook\.com|live\.com|hotmail\.com)$")]
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        // 1: Admin | 2: Votante | 3: Jefe de Junta
        [Range(1, 3)]
        public int RolId { get; set; }
        public bool Estado { get; set; } = true;
        public bool HaVotado { get; set; } = false;
        // Relación con Junta
        public int? JuntaId { get; set; }
        public Junta? Junta { get; set; }
    }
}
