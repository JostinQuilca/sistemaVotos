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
    public class ListasController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public ListasController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/Listas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Lista>>> GetListas()
        {
            return await _context.Listas
                .OrderBy(l => l.Id)
                .ToListAsync();
        }

        // GET: api/Listas/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Lista>> GetLista(int id)
        {
            var lista = await _context.Listas.FindAsync(id);

            if (lista == null)
                return NotFound("Lista no encontrada.");

            return Ok(lista);
        }

        // GET: api/Listas/PorEleccion/3
        [HttpGet("PorEleccion/{eleccionId:int}")]
        public async Task<ActionResult<IEnumerable<Lista>>> GetListasPorEleccion(int eleccionId)
        {
            bool existeEleccion = await _context.Elecciones.AnyAsync(e => e.Id == eleccionId);
            if (!existeEleccion)
                return NotFound("Elección no encontrada.");

            var listas = await _context.Listas
                .Where(l => l.EleccionId == eleccionId)
                .OrderBy(l => l.Id)
                .ToListAsync();

            return Ok(listas);
        }
        // POST: api/Listas
        [HttpPost]
        public async Task<ActionResult<Lista>> PostLista([FromBody] Lista lista)
        {
            if (lista == null)
                return BadRequest("Datos no proporcionados.");

            if (string.IsNullOrWhiteSpace(lista.NombreLista))
                return BadRequest("NombreLista es obligatorio.");

            if (lista.EleccionId <= 0)
                return BadRequest("EleccionId inválido.");

            bool existeEleccion = await _context.Elecciones.AnyAsync(e => e.Id == lista.EleccionId);
            if (!existeEleccion)
                return BadRequest("La elección no existe.");

            var nueva = new Lista
            {
                NombreLista = lista.NombreLista.Trim(),
                LogoUrl = lista.LogoUrl?.Trim() ?? string.Empty,
                EleccionId = lista.EleccionId
            };

            _context.Listas.Add(nueva);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLista), new { id = nueva.Id }, nueva);
        }
        // No permito cambiar EleccionId por PUT (para evitar mover listas entre elecciones sin querer).
        // PUT: api/Listas/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutLista(int id, [FromBody] Lista cambios)
        {
            if (cambios == null)
                return BadRequest("Datos no proporcionados.");

            var lista = await _context.Listas.FindAsync(id);
            if (lista == null)
                return NotFound("Lista no encontrada.");

            if (string.IsNullOrWhiteSpace(cambios.NombreLista))
                return BadRequest("NombreLista es obligatorio.");

            lista.NombreLista = cambios.NombreLista.Trim();
            lista.LogoUrl = cambios.LogoUrl?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Protección: si hay candidatos ligados a esa lista, no la dejo borrar.
        // DELETE: api/Listas/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteLista(int id)
        {
            var lista = await _context.Listas.FindAsync(id);
            if (lista == null)
                return NotFound("Lista no encontrada.");

            bool tieneCandidatos = await _context.Candidatos.AnyAsync(c => c.ListaId == id);
            if (tieneCandidatos)
                return Conflict("No se puede eliminar: la lista tiene candidatos registrados.");

            _context.Listas.Remove(lista);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
