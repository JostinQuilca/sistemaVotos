using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos.DTOs;
using SistemaVotoAPI.Security;
using System;
using System.Threading.Tasks;
using System.Linq;

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
            // 1. Validaciones básicas
            if (request == null || string.IsNullOrEmpty(request.Cedula) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Datos de inicio de sesión incompletos.");
            }

            try
            {
                // 2. Buscar al usuario por Cédula (ignorando espacios)
                var usuario = await _context.Votantes
                    .FirstOrDefaultAsync(v => v.Cedula == request.Cedula.Trim() && v.Estado);

                if (usuario == null)
                {
                    return Unauthorized("Usuario no encontrado o inactivo.");
                }

                bool accesoConcedido = false;
                string passIngresada = request.Password.Trim();

                // ---------------------------------------------------------
                // NIVEL 1: Verificar Contraseña en Texto Plano (Lo más probable en tus datos actuales)
                // ---------------------------------------------------------
                if (usuario.Password == passIngresada)
                {
                    accesoConcedido = true;
                }

                // ---------------------------------------------------------
                // NIVEL 2: Verificar Contraseña Encriptada (Si usaste el Hasher)
                // ---------------------------------------------------------
                if (!accesoConcedido)
                {
                    // El try-catch evita que explote si la contraseña en BD no es un hash válido
                    try
                    {
                        if (PasswordHasher.Verify(passIngresada, usuario.Password))
                        {
                            accesoConcedido = true;
                        }
                    }
                    catch { /* No era un hash, ignoramos este error */ }
                }

                // ---------------------------------------------------------
                // NIVEL 3: Verificar si es un Token de Acceso (Para votar)
                // ---------------------------------------------------------
                if (!accesoConcedido)
                {
                    // Buscamos si la "contraseña" escrita es en realidad un Token válido
                    var tokenValido = await _context.TokensAcceso
                        .FirstOrDefaultAsync(t => t.VotanteId == usuario.Cedula
                                               && t.Codigo == passIngresada
                                               && t.EsValido);

                    if (tokenValido != null)
                    {
                        accesoConcedido = true;
                        // Opcional: Si quieres quemar el token al usarlo, descomenta esto:
                        // tokenValido.EsValido = false;
                        // await _context.SaveChangesAsync();
                    }
                }

                // ---------------------------------------------------------
                // RESULTADO FINAL
                // ---------------------------------------------------------
                if (!accesoConcedido)
                {
                    return Unauthorized("Cédula o contraseña incorrecta.");
                }

                // Armar la respuesta
                var response = new LoginResponseDto
                {
                    Cedula = usuario.Cedula,
                    NombreCompleto = usuario.NombreCompleto ?? "Sin nombre",
                    Email = usuario.Email ?? "Sin email",
                    FotoUrl = usuario.FotoUrl,
                    RolId = usuario.RolId,
                    // Conversión segura de long? a int?
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