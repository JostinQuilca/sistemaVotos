namespace SistemaVotoModelos.DTOs;

public class JuntaDetalleDto
{
    public int Id { get; set; }
    public int NumeroMesa { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public string NombreJefe { get; set; } = string.Empty;
    public int EstadoJunta { get; set; } // <--- DEBE SER INT
}