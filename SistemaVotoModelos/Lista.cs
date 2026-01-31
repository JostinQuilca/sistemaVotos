namespace SistemaVotoModelos;

public class Lista
{
    public int Id { get; set; }

    public string NombreLista { get; set; } = null!;
    public string LogoUrl { get; set; } = null!;

    public int EleccionId { get; set; }
    public Eleccion? Eleccion { get; set; }

    public ICollection<Candidato>? Candidatos { get; set; }
    public ICollection<VotoAnonimo>? VotosAnonimos { get; set; }
}
