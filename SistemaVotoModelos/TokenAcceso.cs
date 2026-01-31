namespace SistemaVotoModelos;

public class TokenAcceso
{
    public int Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string VotanteId { get; set; } = null!;
    public Votante? Votante { get; set; }

    public bool EsValido { get; set; }
    public DateTime FechaCreacion { get; set; }
}
