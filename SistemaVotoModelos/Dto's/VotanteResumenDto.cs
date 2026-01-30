using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaVotoModelos.DTOs;

public class VotanteResumenDto
{
    public string Cedula { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RolId { get; set; }
    public bool Estado { get; set; }
    public string? NombreJunta { get; set; } // Opcional para mostrar en la tabla
}