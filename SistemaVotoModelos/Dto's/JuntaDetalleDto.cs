using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaVotoModelos.DTOs;

public class JuntaDetalleDto
{
    public int Id { get; set; }
    public int NumeroMesa { get; set; }
    public string Ubicacion { get; set; } = string.Empty; // Ej: "Imbabura - Ibarra"
    public string NombreJefe { get; set; } = string.Empty;
    public int EstadoJunta { get; set; }
}