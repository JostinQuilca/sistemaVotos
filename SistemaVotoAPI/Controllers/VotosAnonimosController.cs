using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.Security;
using SistemaVotoModelos;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotosAnonimosController : ControllerBase
    {
        private readonly APIVotosDbContext _context;
        private readonly EmailService _emailService;

        public VotosAnonimosController(APIVotosDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<ActionResult<VotoAnonimo>> PostVotoAnonimo(VotoAnonimo votoAnonimo)
        {
            // 1. Validar Elección
            var eleccion = await _context.Elecciones.FindAsync(votoAnonimo.EleccionId);
            if (eleccion == null) return BadRequest("Elección no válida");

            // 2. Guardar Voto
            votoAnonimo.FechaVoto = DateTime.UtcNow;
            _context.VotosAnonimos.Add(votoAnonimo);
            await _context.SaveChangesAsync();

            // 3. Enviar Correo (Bloque Try/Catch para no fallar si no hay configuración)
            try
            {
                // Aquí iría la lógica de envío si tuviéramos el email del votante.
            }
            catch { }

            return Ok("Voto registrado correctamente");
        }
    }
}