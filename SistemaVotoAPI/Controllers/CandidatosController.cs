using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CandidatosController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public CandidatosController(APIVotosDbContext context)
        {
            _context = context;
        }

        // Aquí devuelvo los candidatos de una elección con su info de votante y lista.
        [HttpGet("PorEleccion/{eleccionId:int}")]
        public async Task<IActionResult> PorEleccion(int eleccionId)
        {
            bool existeEleccion = await _context.Elecciones.AnyAsync(e => e.Id == eleccionId);
            if (!existeEleccion)
                return NotFound("Elección no encontrada.");

            var lista = await _context.Candidatos
                .Where(c => c.EleccionId == eleccionId)
                .Include(c => c.Votante)
                .Include(c => c.Lista)
                .OrderBy(c => c.RolPostulante)
                .ThenBy(c => c.Votante != null ? c.Votante.NombreCompleto : "")
                .ToListAsync();

            return Ok(lista);
        }

        // aquí creo un candidato solo si la elección está en CONFIGURACION y el votante es válido (no admin/jefe).
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Candidato candidato)
        {
            if (candidato == null)
                return BadRequest("Datos no proporcionados.");

            if (string.IsNullOrWhiteSpace(candidato.Cedula))
                return BadRequest("Cédula obligatoria.");

            if (candidato.EleccionId <= 0)
                return BadRequest("Elección inválida.");

            if (candidato.ListaId <= 0)
                return BadRequest("Lista inválida.");

            if (string.IsNullOrWhiteSpace(candidato.RolPostulante))
                return BadRequest("Rol postulante obligatorio.");

            string cedula = candidato.Cedula.Trim();
            string rolPostulante = candidato.RolPostulante.Trim();

            var eleccion = await _context.Elecciones.FindAsync(candidato.EleccionId);
            if (eleccion == null)
                return BadRequest("La elección no existe.");

            if (eleccion.Estado != "CONFIGURACION")
                return Conflict("Solo se pueden registrar candidatos cuando la elección está en CONFIGURACION.");

            var votante = await _context.Votantes.FindAsync(cedula);
            if (votante == null)
                return BadRequest("El votante no existe.");

            if (votante.RolId == 1 || votante.RolId == 3)
                return Conflict("Un administrador o jefe de junta no puede ser candidato.");

            // La lista debe existir y pertenecer a esa elección
            var lista = await _context.Listas.FirstOrDefaultAsync(l => l.Id == candidato.ListaId);
            if (lista == null)
                return BadRequest("La lista asignada no existe.");

            if (lista.EleccionId != candidato.EleccionId)
                return BadRequest("La lista no pertenece a esa elección.");

            bool duplicado = await _context.Candidatos.AnyAsync(c =>
                c.Cedula == cedula && c.EleccionId == candidato.EleccionId);

            if (duplicado)
                return Conflict("Ese votante ya es candidato en esta elección.");

            var nuevo = new Candidato
            {
                Cedula = cedula,
                EleccionId = candidato.EleccionId,
                ListaId = candidato.ListaId,
                RolPostulante = rolPostulante
            };

            _context.Candidatos.Add(nuevo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(PorEleccion), new { eleccionId = nuevo.EleccionId }, nuevo);
        }

        // Aquí permito editar solo ListaId y RolPostulante (no dejo tocar cédula ni elección).
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Editar(int id, [FromBody] Candidato cambios)
        {
            if (cambios == null)
                return BadRequest("Datos no proporcionados.");

            if (cambios.ListaId <= 0)
                return BadRequest("Lista inválida.");

            if (string.IsNullOrWhiteSpace(cambios.RolPostulante))
                return BadRequest("Rol postulante obligatorio.");

            var candidato = await _context.Candidatos.FindAsync(id);
            if (candidato == null)
                return NotFound("Candidato no encontrado.");

            var eleccion = await _context.Elecciones.FindAsync(candidato.EleccionId);
            if (eleccion == null)
                return BadRequest("La elección no existe.");

            if (eleccion.Estado != "CONFIGURACION")
                return Conflict("Solo se puede editar candidatos cuando la elección está en CONFIGURACION.");

            // La lista debe existir y pertenecer a la misma elección del candidato
            var lista = await _context.Listas.FirstOrDefaultAsync(l => l.Id == cambios.ListaId);
            if (lista == null)
                return BadRequest("La lista asignada no existe.");

            if (lista.EleccionId != candidato.EleccionId)
                return BadRequest("La lista no pertenece a la elección del candidato.");

            candidato.ListaId = cambios.ListaId;
            candidato.RolPostulante = cambios.RolPostulante.Trim();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Aquí elimino la postulación, pero bloqueo si la elección ya no está en CONFIGURACION.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var candidato = await _context.Candidatos.FindAsync(id);
            if (candidato == null)
                return NotFound("Candidato no encontrado.");

            var eleccion = await _context.Elecciones.FindAsync(candidato.EleccionId);
            if (eleccion == null)
                return BadRequest("La elección no existe.");

            if (eleccion.Estado != "CONFIGURACION")
                return Conflict("Solo se puede eliminar candidatos cuando la elección está en CONFIGURACION.");

            _context.Candidatos.Remove(candidato);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
