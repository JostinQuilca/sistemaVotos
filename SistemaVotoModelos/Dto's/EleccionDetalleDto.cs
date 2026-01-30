using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaVotoModelos.DTOs;

public class EleccionDetalleDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty; // Activa, Finalizada, etc.
}