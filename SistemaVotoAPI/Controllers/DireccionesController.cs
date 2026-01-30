using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DireccionesController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public DireccionesController(APIVotosDbContext context)
        {
            _context = context;
        }

        // Para obtener todas las direcciones ordenadas por Id para mostrarlas de forma consistente
        [HttpGet]
        public async Task<IActionResult> ObtenerTodas()
        {
            var lista = await _context.Direcciones
                .OrderBy(d => d.Id)
                .ToListAsync();

            return Ok(lista);
        }

        // Crear una nueva dirección validando que no exista y generando el Id jerárquico
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Direccion dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Provincia) ||
                string.IsNullOrWhiteSpace(dto.Canton) ||
                string.IsNullOrWhiteSpace(dto.Parroquia))
            {
                return BadRequest("Provincia, Cantón y Parroquia son obligatorios.");
            }

            bool existe = await _context.Direcciones.AnyAsync(d =>
                d.Provincia == dto.Provincia &&
                d.Canton == dto.Canton &&
                d.Parroquia == dto.Parroquia
            );

            if (existe)
                return Conflict("La dirección ya existe.");

            int nuevoId = await GenerarIdAsync(
                dto.Provincia.Trim(),
                dto.Canton.Trim(),
                dto.Parroquia.Trim()
            );

            var direccion = new Direccion
            {
                Id = nuevoId,
                Provincia = dto.Provincia.Trim(),
                Canton = dto.Canton.Trim(),
                Parroquia = dto.Parroquia.Trim()
            };

            _context.Direcciones.Add(direccion);
            await _context.SaveChangesAsync();

            return Ok(direccion);
        }

        // Aquí elimino una dirección solo por Id, asumiendo que no está referenciada en otras tablas
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var direccion = await _context.Direcciones.FindAsync(id);

            if (direccion == null)
                return NotFound("La dirección no existe.");

            _context.Direcciones.Remove(direccion);
            await _context.SaveChangesAsync();

            return Ok("Dirección eliminada correctamente.");
        }

        // Aquí genero el Id concatenando provincia, cantón y parroquia en el orden en que aparecen
        private async Task<int> GenerarIdAsync(string provincia, string canton, string parroquia)
        {
            var provincias = await _context.Direcciones
                .Select(d => d.Provincia)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            int provId = provincias.Contains(provincia)
                ? provincias.IndexOf(provincia) + 1
                : provincias.Count + 1;

            var cantones = await _context.Direcciones
                .Where(d => d.Provincia == provincia)
                .Select(d => d.Canton)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            int cantId = cantones.Contains(canton)
                ? cantones.IndexOf(canton) + 1
                : cantones.Count + 1;

            var parroquias = await _context.Direcciones
                .Where(d => d.Provincia == provincia && d.Canton == canton)
                .Select(d => d.Parroquia)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            int parrId = parroquias.Contains(parroquia)
                ? parroquias.IndexOf(parroquia) + 1
                : parroquias.Count + 1;

            return int.Parse($"{provId:D2}{cantId:D2}{parrId:D2}");
        }
    }
}
