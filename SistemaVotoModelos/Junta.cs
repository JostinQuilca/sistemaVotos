namespace SistemaVotoModelos;

public class Junta
{
    public long Id { get; set; }   // ← IMPORTANTE: long

    public int NumeroMesa { get; set; }

    public int DireccionId { get; set; }
    public Direccion Direccion { get; set; }

    public int EleccionId { get; set; }
    public Eleccion Eleccion { get; set; }

    public string? JefeDeJuntaId { get; set; }
    public Votante? JefeDeJunta { get; set; }

    public int Estado { get; set; }

    public ICollection<Votante> Votantes { get; set; }
}
