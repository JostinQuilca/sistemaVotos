using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using SistemaVotoAPI.Security; // Asegúrate de que este namespace coincida con tu clase PasswordHasher
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
            return await _context.Votantes
                .OrderBy(v => v.RolId) // Ordena: 1=Admin, 2=Votante, 3=Jefe
                .ToListAsync();
        }

        // GET: api/Votantes/cedula
        [HttpGet("{cedula}")]
        public async Task<ActionResult<Votante>> GetVotante(string cedula)
        {
            var votante = await _context.Votantes.FindAsync(cedula);

            if (votante == null)
                return NotFound();

            return votante;
        }

        // GET: api/Votantes/PorJunta/id
        [HttpGet("PorJunta/{juntaId}")]
        public async Task<ActionResult<IEnumerable<Votante>>> GetVotantesPorJunta(int juntaId)
        {
            return await _context.Votantes
                .Where(v => v.JuntaId == juntaId)
                .ToListAsync();
        }

        // POST: api/Votantes (CREAR)
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
                votante.JuntaId = null;
            }

            // HASHEAR CONTRASEÑA NUEVA (Usando el nuevo hasher de 100k)
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

        // PUT: api/Votantes/cedula (EDITAR)
        [HttpPut("{cedula}")]
        public async Task<IActionResult> PutVotante(string cedula, Votante votante)
        {
            if (cedula != votante.Cedula)
                return BadRequest("La cédula no coincide.");

            // 1. Buscamos el usuario original en la BD (sin rastrear)
            var existente = await _context.Votantes.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Cedula == cedula);

            if (existente == null)
                return NotFound();

            // Validación: Candidato no puede ser Admin/Jefe
            if (votante.RolId != existente.RolId && (votante.RolId == 1 || votante.RolId == 3))
            {
                bool esCandidato = await _context.Candidatos.AnyAsync(c => c.Cedula == cedula);
                if (esCandidato)
                    return Conflict("Un candidato no puede ser administrador ni jefe de junta.");
            }

            _context.Entry(votante).State = EntityState.Modified;

            // --- PROTECCIÓN CONTRA EL BUG DE BLOQUEO ---
            // Aseguramos que el estado sea TRUE al editar (o mantenemos el existente)
            votante.Estado = true;
            votante.HaVotado = existente.HaVotado;

            // Manejo correcto de JuntaId
            if (votante.JuntaId.HasValue && votante.JuntaId > 0)
                votante.JuntaId = votante.JuntaId;
            else
                votante.JuntaId = null;

            // LÓGICA DE CONTRASEÑA SEGURA
            if (string.IsNullOrWhiteSpace(votante.Password))
            {
                // Si viene vacía, MANTENEMOS la que ya tenía
                votante.Password = existente.Password;
            }
            else
            {
                // Si viene nueva, la HASHEAMOS con el nuevo algoritmo
                votante.Password = PasswordHasher.Hash(votante.Password.Trim());
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Votantes.AnyAsync(e => e.Cedula == cedula))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/Votantes/cedula
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