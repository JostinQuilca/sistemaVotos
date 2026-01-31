using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos.DTOs;
using SistemaVotoAPI.Security;
using System;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public AutController(APIVotosDbContext context)
        {
            _context = context;
        }

        [HttpPost("LoginGestion")]
        public async Task<IActionResult> LoginGestion([FromBody] LoginRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Cedula) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Datos de inicio de sesión incompletos.");
            }

            try
            {
                var usuario = await _context.Votantes
                    .FirstOrDefaultAsync(v => v.Cedula == request.Cedula && v.Estado);

                if (usuario == null)
                {
                    return Unauthorized("Usuario no encontrado o inactivo.");
                }

                bool esValida = PasswordHasher.Verify(request.Password.Trim(), usuario.Password);

                if (!esValida)
                {
                    return Unauthorized("Cédula o contraseña incorrecta.");
                }

                var response = new LoginResponseDto
                {
                    Cedula = usuario.Cedula,
                    NombreCompleto = usuario.NombreCompleto ?? "Sin nombre",
                    Email = usuario.Email ?? "Sin email",
                    FotoUrl = usuario.FotoUrl,
                    RolId = usuario.RolId,

                    // 🔴 AQUÍ ESTABA EL ERROR CS0266
                    // usuario.JuntaId es long?
                    // DTO espera int?
                    JuntaId = usuario.JuntaId.HasValue ? (int?)usuario.JuntaId.Value : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPut("MarcarVoto/{cedula}")]
        public async Task<IActionResult> MarcarVoto(string cedula)
        {
            if (string.IsNullOrEmpty(cedula))
                return BadRequest("Cédula inválida");

            var usuario = await _context.Votantes
                .FirstOrDefaultAsync(v => v.Cedula.Trim() == cedula.Trim());

            if (usuario == null)
                return NotFound($"Usuario con cédula {cedula} no encontrado.");

            usuario.HaVotado = true;
            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Usuario marcado como VOTADO exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar en BD: {ex.Message}");
            }
        }

        [HttpGet("VerificarEstado/{cedula}")]
        public async Task<IActionResult> VerificarEstado(string cedula)
        {
            var usuario = await _context.Votantes
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Cedula.Trim() == cedula.Trim());

            if (usuario == null)
                return NotFound("Usuario no encontrado");

            return Ok(usuario.HaVotado);
        }
    }
}
