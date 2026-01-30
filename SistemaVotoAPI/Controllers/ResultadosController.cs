using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultadosController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public ResultadosController(APIVotosDbContext context)
        {
            _context = context;
        }

        // RESULTADOS POR JUNTA
        [HttpGet("PorJunta/{juntaId}")]
        public async Task<IActionResult> ResultadosPorJunta(int juntaId)
        {
            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null)
                return NotFound("Junta no encontrada");

            var resultados = await _context.VotosAnonimos
                .Where(v => v.NumeroMesa == junta.NumeroMesa)
                .GroupBy(v => v.ListaId)
                .Select(g => new
                {
                    ListaId = g.Key,
                    TotalVotos = g.Count()
                })
                .ToListAsync();

            return Ok(resultados);
        }

        // RESULTADOS POR LISTA
        [HttpGet("PorLista/{listaId}")]
        public async Task<IActionResult> ResultadosPorLista(int listaId)
        {
            var total = await _context.VotosAnonimos
                .CountAsync(v => v.ListaId == listaId);

            return Ok(new
            {
                ListaId = listaId,
                TotalVotos = total
            });
        }

        // RESULTADOS POR DIRECCIÓN
        [HttpGet("PorDireccion")]
        public async Task<IActionResult> ResultadosPorDireccion(
            string provincia,
            string? canton = null,
            string? parroquia = null)
        {
            var query = _context.VotosAnonimos
                .Join(
                    _context.Direcciones,
                    voto => voto.DireccionId,
                    direccion => direccion.Id,
                    (voto, direccion) => new { voto, direccion }
                )
                .Where(x => x.direccion.Provincia == provincia);

            if (!string.IsNullOrEmpty(canton))
                query = query.Where(x => x.direccion.Canton == canton);

            if (!string.IsNullOrEmpty(parroquia))
                query = query.Where(x => x.direccion.Parroquia == parroquia);

            var resultados = await query
                .GroupBy(x => x.voto.ListaId)
                .Select(g => new
                {
                    ListaId = g.Key,
                    TotalVotos = g.Count()
                })
                .ToListAsync();

            return Ok(resultados);
        }

        // VALIDACIÓN PARA CIERRE DE JUNTA
        [HttpGet("ValidarCierreJunta/{juntaId}")]
        public async Task<IActionResult> ValidarCierreJunta(int juntaId)
        {
            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null)
                return NotFound("Junta no encontrada");

            var totalVotantes = await _context.Votantes
                .CountAsync(v => v.JuntaId == juntaId);

            var totalVotos = await _context.VotosAnonimos
                .CountAsync(v => v.NumeroMesa == junta.NumeroMesa);

            return Ok(new
            {
                Junta = junta.NumeroMesa,
                TotalVotantes = totalVotantes,
                TotalVotosEmitidos = totalVotos,
                Coinciden = totalVotantes == totalVotos
            });
        }
    }
}
