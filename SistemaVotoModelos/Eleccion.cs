namespace SistemaVotoModelos;

public class Eleccion
{
    public int Id { get; set; }

    public string Titulo { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = null!;

    public ICollection<Junta>? Juntas { get; set; }
    public ICollection<Lista>? Listas { get; set; }
    public ICollection<Candidato>? Candidatos { get; set; }
    public ICollection<VotoAnonimo>? VotosAnonimos { get; set; }
}
