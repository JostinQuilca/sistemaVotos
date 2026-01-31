using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos.DTOs;
using SistemaVotoAPI.Security; // Asegúrate de que este namespace coincida con tu clase PasswordHasher
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
                // NIVEL 1: Verificar Contraseña en Texto Plano (Recuperación/Legacy)
                // ---------------------------------------------------------
                if (usuario.Password == passIngresada)
                {
                    accesoConcedido = true;
                }

                // ---------------------------------------------------------
                // NIVEL 2: Verificar Contraseña Encriptada (Hash Nuevo)
                // ---------------------------------------------------------
                if (!accesoConcedido)
                {
                    try
                    {
                        // Usa tu PasswordHasher actualizado (100k iteraciones)
                        if (PasswordHasher.Verify(passIngresada, usuario.Password))
                        {
                            accesoConcedido = true;
                        }
                    }
                    catch
                    {
                        // Si el formato en BD no es un hash válido (ej. texto antiguo), ignoramos el error
                    }
                }

                // ---------------------------------------------------------
                // NIVEL 3: Verificar si es un Token de Acceso (Para votar)
                // ---------------------------------------------------------
                if (!accesoConcedido)
                {
                    var tokenValido = await _context.TokensAcceso
                        .FirstOrDefaultAsync(t => t.VotanteId == usuario.Cedula
                                               && t.Codigo == passIngresada
                                               && t.EsValido);

                    if (tokenValido != null)
                    {
                        accesoConcedido = true;
                        // Opcional: tokenValido.EsValido = false; await _context.SaveChangesAsync();
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

        // Diagnóstico (Opcional, útil si algo falla de nuevo)
        [HttpGet("VerUsuario/{cedula}")]
        public async Task<IActionResult> VerUsuario(string cedula)
        {
            var v = await _context.Votantes.AsNoTracking().FirstOrDefaultAsync(x => x.Cedula == cedula);
            if (v == null) return NotFound("No existe");
            return Ok(new
            {
                Cedula = v.Cedula,
                PasswordGuardada = v.Password,
                Estado = v.Estado,
                Rol = v.RolId
            });
        }
    }
}