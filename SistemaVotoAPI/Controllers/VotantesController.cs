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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Votante>>> GetVotantes()
        {
            return await _context.Votantes.ToListAsync();
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

            /*
             Lógica de negocio:
             Validación de roles permitidos en el sistema
            */
            if (votante.RolId < 1 || votante.RolId > 3)
                return BadRequest("Rol inválido.");

            /*
             Lógica de negocio
             Un candidato no puede ser creado como administrador ni jefe de junta
            */
            if (votante.RolId == 1 || votante.RolId == 3)
            {
                bool existeComoCandidato = await _context.Candidatos
                    .AnyAsync(c => c.Cedula == votante.Cedula);

                if (existeComoCandidato)
                    return Conflict("Un candidato no puede ser administrador ni jefe de junta.");
            }

            /*
             Lógica de consistencia
             Si se envía una junta, debe existir
            */
            if (votante.JuntaId.HasValue)
            {
                bool existeJunta = await _context.Juntas
                    .AnyAsync(j => j.Id == votante.JuntaId.Value);

                if (!existeJunta)
                    return BadRequest("La junta asignada no existe.");
            }

            /*
             Lógica de seguridad
             La contraseña se almacena siempre como hash
            */
            votante.Password = PasswordHasher.Hash(votante.Password);

            /*
             Lógica de estado inicial
             Todo votante se registra activo y sin haber votado
            */
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
                return BadRequest();

            var existente = await _context.Votantes.FindAsync(cedula);
            if (existente == null)
                return NotFound();

            /*
             Lógica de negocio
             No se permite asignar rol de administrador o jefe de junta
             a una persona que ya es candidata
            */
            if (votante.RolId != existente.RolId &&
                (votante.RolId == 1 || votante.RolId == 3))
            {
                bool esCandidato = await _context.Candidatos
                    .AnyAsync(c => c.Cedula == cedula);

                if (esCandidato)
                    return Conflict("Un candidato no puede ser administrador ni jefe de junta.");
            }

            existente.NombreCompleto = votante.NombreCompleto;
            existente.Email = votante.Email;
            existente.FotoUrl = votante.FotoUrl;
            existente.RolId = votante.RolId;
            existente.Estado = votante.Estado;
            existente.JuntaId = votante.JuntaId;

            /*
             Lógica de seguridad
             Solo se vuelve a hashear la contraseña si se envía una nueva
            */
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
