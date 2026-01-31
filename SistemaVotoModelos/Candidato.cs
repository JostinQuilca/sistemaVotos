namespace SistemaVotoModelos;

public class Candidato
{
    public int Id { get; set; }

    public string Cedula { get; set; } = null!;
    public Votante? Votante { get; set; }

    public int ListaId { get; set; }
    public Lista? Lista { get; set; }

    public int EleccionId { get; set; }
    public Eleccion? Eleccion { get; set; }

    public string RolPostulante { get; set; } = null!;
}
