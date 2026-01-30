using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaVotoModelos.DTOs;

public class TokenVotacionDto
{
    public string CedulaVotante { get; set; } = string.Empty;
    public string CodigoToken { get; set; } = string.Empty;
    public DateTime Expiracion { get; set; }
}