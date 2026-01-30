using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaVotoModelos.DTOs;

public class EleccionCrearDto
{
    public string Titulo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}