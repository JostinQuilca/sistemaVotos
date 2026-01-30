using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaVotoModelos
{
    public class VotoAnonimo
    {
        [Key]
        public int Id { get; set; }
        public DateTime FechaVoto { get; set; } = DateTime.UtcNow;
        // Elección a la que pertenece el voto
        public int EleccionId { get; set; }
        // Ubicación para reportes
        public int DireccionId { get; set; }
        public int NumeroMesa { get; set; }
        // Información del voto
        public int ListaId { get; set; }
        // Información del candidato (DESCRIPTIVA, NO RELACIONAL)
        public string CedulaCandidato { get; set; } = string.Empty;
        public string RolPostulante { get; set; } = string.Empty;
    }
}
