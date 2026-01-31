using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoAPI.Security;
using SistemaVotoModelos;
using System;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokensController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public TokensController(APIVotosDbContext context)
        {
            _context = context;
        }

        // POST: api/Tokens/Generar/1002003004
        [HttpPost("Generar/{cedula}")]
        public async Task<IActionResult> GenerarToken(string cedula)
        {
            // 1. Validar que exista el votante
            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null) return NotFound("Votante no encontrado");

            // 2. Validar que no haya votado ya
            if (votante.HaVotado)
                return BadRequest("El usuario ya ha ejercido su voto.");

            // 3. Generar código aleatorio de 6 dígitos
            var random = new Random();
            string codigo = random.Next(100000, 999999).ToString();

            // 4. Guardar en el historial de Tokens
            var token = new TokenAcceso
            {
                VotanteId = cedula,
                Codigo = codigo,
                EsValido = true,
                FechaCreacion = DateTime.UtcNow
            };
            _context.TokensAcceso.Add(token);

            // 5. ACTUALIZAR LA CONTRASEÑA DEL VOTANTE (HASH)
            votante.Password = PasswordHasher.Hash(codigo);

            await _context.SaveChangesAsync();

            // Retornamos el código para que el Jefe de Junta se lo dé al votante
            return Ok(new { token = codigo, mensaje = "Acceso habilitado correctamente" });
        }
    }
}