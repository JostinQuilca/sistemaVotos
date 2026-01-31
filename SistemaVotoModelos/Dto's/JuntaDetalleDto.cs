namespace SistemaVotoModelos.DTOs;

public class JuntaDetalleDto
{
    public long Id { get; set; }          // bigint de la BD
    public int NumeroMesa { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public string NombreJefe { get; set; } = string.Empty;
    public int EstadoJunta { get; set; }
}
