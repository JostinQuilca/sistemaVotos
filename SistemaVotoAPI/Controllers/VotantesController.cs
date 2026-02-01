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
        // PUT: api/Votantes/cedula (EDITAR SEGURO)
        [HttpPut("{cedula}")]
        public async Task<IActionResult> PutVotante(string cedula, Votante votante)
        {
            if (cedula != votante.Cedula) return BadRequest("Cédula no coincide.");

            // 1. Buscamos el usuario ORIGINAL en la Base de Datos (sin tracking para comparar)
            var existente = await _context.Votantes.AsNoTracking().FirstOrDefaultAsync(v => v.Cedula == cedula);

            if (existente == null) return NotFound("Usuario no existe.");

            // Validaciones de Roles (Mantenemos tu lógica)
            if (votante.RolId != existente.RolId && (votante.RolId == 1 || votante.RolId == 3))
            {
                if (await _context.Candidatos.AnyAsync(c => c.Cedula == cedula))
                    return Conflict("Un candidato no puede ser administrador ni jefe de junta.");
            }

            _context.Entry(votante).State = EntityState.Modified;

            // --- PROTECCIÓN CRÍTICA CONTRA DOBLE ENCRIPTACIÓN ---

            // Caso A: Si la contraseña viene vacía, recuperamos la vieja.
            if (string.IsNullOrWhiteSpace(votante.Password))
            {
                votante.Password = existente.Password;
            }
            // Caso B: Si la contraseña que envían ES IDÉNTICA a la que ya existe (el Hash),
            // SIGNIFICA QUE NO LA CAMBIARON. ¡No la volvemos a encriptar!
            else if (votante.Password == existente.Password)
            {
                // Dejamos la contraseña tal cual, sin tocarla.
                votante.Password = existente.Password;
            }
            // Caso C: Es diferente y tiene texto. Es una NUEVA contraseña real (ej: "hola123").
            else
            {
                votante.Password = PasswordHasher.Hash(votante.Password.Trim());
            }
            // ----------------------------------------------------

            // Aseguramos mantener datos críticos si vienen nulos
            votante.Estado = true; // Siempre activo al editar para evitar bloqueos

            // Mantenemos el estado del voto real si no se especificó
            // (Esto evita que se resetee el voto accidentalmente)
            if (!votante.HaVotado && existente.HaVotado)
            {
                votante.HaVotado = true;
            }

            // Manejo de Junta
            if (votante.JuntaId.HasValue && votante.JuntaId > 0)
                votante.JuntaId = votante.JuntaId;
            else
                votante.JuntaId = null;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Votantes.AnyAsync(e => e.Cedula == cedula)) return NotFound();
                else throw;
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