namespace SistemaVotoModelos.DTOs
{
    public class LoginResponseDto
    {
        public string Cedula { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        public int RolId { get; set; }
        public int? JuntaId { get; set; }
    }
}