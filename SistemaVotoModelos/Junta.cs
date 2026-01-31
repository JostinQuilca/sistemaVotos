using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Necesario para [ForeignKey]

namespace SistemaVotoModelos
{
    public class Junta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NumeroMesa { get; set; }

        [Required]
        public int DireccionId { get; set; }

        [ForeignKey("DireccionId")]
        public Direccion? Direccion { get; set; }

        // --- RELACIÓN CON EL JEFE DE JUNTA ---
        // Usamos string? porque la cédula es string y puede ser null (sin asignar)
        public string? JefeDeJuntaId { get; set; }

        [ForeignKey("JefeDeJuntaId")]
        public Votante? JefeDeJunta { get; set; }

        // --- ESTADO (NÚMERO) ---
        // 1=Cerrada, 2=Abierta, 3=Pendiente, 4=Aprobada
        public int Estado { get; set; } = 1;

        // Relación inversa (opcional pero útil)
        public ICollection<Votante> Votantes { get; set; } = new List<Votante>();
    }
}