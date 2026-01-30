using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaVotoModelos
{
    public class Eleccion
    {
        [Key]
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        // Estados: Para poder configurar el estado de la elección de forma automatica
        // CONFIGURACION, ACTIVA, FINALIZADA
        public string Estado { get; set; } = "CONFIGURACION";

    }
}