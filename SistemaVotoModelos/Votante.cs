namespace SistemaVotoModelos;

public class Votante
{
    public string Cedula { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FotoUrl { get; set; } = null!;

    public int RolId { get; set; }
    public bool Estado { get; set; }
    public bool HaVotado { get; set; }

    public long? JuntaId { get; set; }
    public Junta? Junta { get; set; }

    public ICollection<TokenAcceso>? TokensAcceso { get; set; }
}
