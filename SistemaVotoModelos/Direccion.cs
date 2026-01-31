namespace SistemaVotoModelos;

public class Direccion
{
    public int Id { get; set; }

    public string Provincia { get; set; } = null!;
    public string Canton { get; set; } = null!;
    public string Parroquia { get; set; } = null!;

    public ICollection<Junta>? Juntas { get; set; }
    public ICollection<VotoAnonimo>? VotosAnonimos { get; set; }
}
