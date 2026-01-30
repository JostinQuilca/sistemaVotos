namespace SistemaVotoModelos.DTOs
{
    public class LoginRequestDto
    {
        public string Cedula { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}