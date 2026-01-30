using System.ComponentModel.DataAnnotations;

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
        public Direccion? Direccion { get; set; }
        // Se asigna después, por eso puede ser null
        public string? JefeDeJuntaId { get; set; }
        public Votante? JefeDeJunta { get; set; }
        // 1=Cerrada | 2=Abierta | 3=Pendiente | 4=Aprobada
        public int Estado { get; set; } = 1;
        public ICollection<Votante> Votantes { get; set; } = new List<Votante>();
    }
}

