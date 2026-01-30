using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos.DTOs;
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

        // ==========================================
        // LOGIN
        // ==========================================
        [HttpPost("LoginGestion")]
        public async Task<IActionResult> LoginGestion([FromBody] LoginRequestDto request)
        {
            // 1. Validar que enviaron datos
            if (request == null || string.IsNullOrEmpty(request.Cedula) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Datos de inicio de sesión incompletos.");
            }

            try
            {
                // 2. Buscar usuario (Debe estar ACTIVO)
                var usuario = await _context.Votantes
                    .FirstOrDefaultAsync(v => v.Cedula == request.Cedula && v.Estado == true);

                if (usuario == null)
                {
                    return Unauthorized("Usuario no encontrado o inactivo.");
                }

                // 3. Validar Contraseña (¡USANDO TRIM!)
                string passIngresado = request.Password.Trim();
                string passEnBD = (usuario.Password ?? "").Trim();

                if (passIngresado != passEnBD)
                {
                    return Unauthorized("Cédula o contraseña incorrecta.");
                }

                // 4. Login Exitoso: Preparamos la respuesta
                var response = new LoginResponseDto
                {
                    Cedula = usuario.Cedula,
                    NombreCompleto = usuario.NombreCompleto ?? "Sin nombre",
                    Email = usuario.Email ?? "Sin email",
                    FotoUrl = usuario.FotoUrl,
                    RolId = usuario.RolId,
                    JuntaId = usuario.JuntaId
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // ==========================================
        // CONTROL DE VOTOS (NUEVO)
        // ==========================================

        // EN: SistemaVotoAPI / Controllers / AutController.cs

        [HttpPut("MarcarVoto/{cedula}")]
        public async Task<IActionResult> MarcarVoto(string cedula)
        {
            if (string.IsNullOrEmpty(cedula)) return BadRequest("Cédula inválida");

            // Buscamos el usuario ignorando espacios
            var usuario = await _context.Votantes
                .FirstOrDefaultAsync(v => v.Cedula.Trim() == cedula.Trim());

            if (usuario == null) return NotFound($"Usuario con cédula {cedula} no encontrado.");

            // Actualizamos
            usuario.HaVotado = true;

            // Forzamos la marca de modificado
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
                .AsNoTracking() // Más rápido
                .FirstOrDefaultAsync(v => v.Cedula.Trim() == cedula.Trim());

            if (usuario == null) return NotFound("Usuario no encontrado");

            return Ok(usuario.HaVotado);
        }
    }
}