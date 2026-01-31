namespace SistemaVotoModelos;

public class VotoAnonimo
{
    public int Id { get; set; }

    public DateTime FechaVoto { get; set; }

    public int EleccionId { get; set; }
    public Eleccion? Eleccion { get; set; }

    public int DireccionId { get; set; }
    public Direccion? Direccion { get; set; }

    public int NumeroMesa { get; set; }

    public int ListaId { get; set; }
    public Lista? Lista { get; set; }

    public string CedulaCandidato { get; set; } = null!;
    public string RolPostulante { get; set; } = null!;
}
