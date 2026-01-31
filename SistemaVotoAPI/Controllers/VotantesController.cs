using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using SistemaVotoAPI.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotantesController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public VotantesController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/Votantes
        // IMPORTANTE: Devuelve TODOS los usuarios para que el Admin los gestione.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Votante>>> GetVotantes()
        {
            return await _context.Votantes
                .OrderBy(v => v.RolId) // Ordena: 1=Admin, 2=Votante, 3=Jefe
                .ToListAsync();
        }

        // GET: api/Votantes/0102030405
        [HttpGet("{cedula}")]
        public async Task<ActionResult<Votante>> GetVotante(string cedula)
        {
            var votante = await _context.Votantes.FindAsync(cedula);

            if (votante == null)
                return NotFound();

            return votante;
        }

        // GET: api/Votantes/PorJunta/3
        [HttpGet("PorJunta/{juntaId}")]
        public async Task<ActionResult<IEnumerable<Votante>>> GetVotantesPorJunta(int juntaId)
        {
            return await _context.Votantes
                .Where(v => v.JuntaId == juntaId)
                .ToListAsync();
        }

        // POST: api/Votantes
        [HttpPost]
        public async Task<ActionResult<Votante>> PostVotante(Votante votante)
        {
            if (await _context.Votantes.AnyAsync(v => v.Cedula == votante.Cedula))
                return Conflict("Ya existe un votante con esa cédula.");

            // Validación de roles permitidos (1=Admin, 2=Votante, 3=Jefe)
            if (votante.RolId < 1 || votante.RolId > 3)
                return BadRequest("Rol inválido.");

            // Un candidato no puede ser Admin (1) ni Jefe (3)
            if (votante.RolId == 1 || votante.RolId == 3)
            {
                bool existeComoCandidato = await _context.Candidatos
                    .AnyAsync(c => c.Cedula == votante.Cedula);

                if (existeComoCandidato)
                    return Conflict("Un candidato no puede ser administrador ni jefe de junta.");
            }

            // Si se envía una junta, debe existir
            if (votante.JuntaId.HasValue && votante.JuntaId > 0)
            {
                bool existeJunta = await _context.Juntas
                    .AnyAsync(j => j.Id == votante.JuntaId.Value);

                if (!existeJunta)
                    return BadRequest("La junta asignada no existe.");
            }
            else
            {
                // Si viene 0 o negativo, lo dejamos nulo
                votante.JuntaId = null;
            }

            // Hashear contraseña si viene
            if (!string.IsNullOrEmpty(votante.Password))
            {
                votante.Password = PasswordHasher.Hash(votante.Password);
            }

            // Estado inicial
            votante.Estado = true;
            votante.HaVotado = false;

            _context.Votantes.Add(votante);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVotante), new { cedula = votante.Cedula }, votante);
        }

        // PUT: api/Votantes/0102030405
        [HttpPut("{cedula}")]
        public async Task<IActionResult> PutVotante(string cedula, Votante votante)
        {
            if (cedula != votante.Cedula)
                return BadRequest("La cédula no coincide.");

            var existente = await _context.Votantes.FindAsync(cedula);
            if (existente == null)
                return NotFound();

            // Validación: Candidato no puede ser Admin/Jefe
            if (votante.RolId != existente.RolId && (votante.RolId == 1 || votante.RolId == 3))
            {
                bool esCandidato = await _context.Candidatos.AnyAsync(c => c.Cedula == cedula);
                if (esCandidato)
                    return Conflict("Un candidato no puede ser administrador ni jefe de junta.");
            }

            existente.NombreCompleto = votante.NombreCompleto;
            existente.Email = votante.Email;
            existente.FotoUrl = votante.FotoUrl;
            existente.RolId = votante.RolId;
            existente.Estado = votante.Estado;

            // Manejo correcto de JuntaId (permitir desasignar con null o 0)
            if (votante.JuntaId.HasValue && votante.JuntaId > 0)
                existente.JuntaId = votante.JuntaId;
            else
                existente.JuntaId = null;

            // Solo actualizamos contraseña si viene texto nuevo
            if (!string.IsNullOrWhiteSpace(votante.Password))
            {
                existente.Password = PasswordHasher.Hash(votante.Password);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Votantes/0102030405
        [HttpDelete("{cedula}")]
        public async Task<IActionResult> DeleteVotante(string cedula)
        {
            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null)
                return NotFound();

            _context.Votantes.Remove(votante);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}