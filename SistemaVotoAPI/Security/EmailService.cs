using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Security
{
    public class EmailService
    {
        public async Task EnviarCertificado(string destinatario, string nombre, string eleccion)
        {
            // Lógica pendiente de configuración
            await Task.CompletedTask;
        }
    }
}