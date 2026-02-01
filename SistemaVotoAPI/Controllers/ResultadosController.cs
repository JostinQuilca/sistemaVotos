using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using System;
using System.Collections.Generic;
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

        // --------------------------------------------------------------------
        // 1. RESULTADOS EN VIVO (GLOBALES POR ELECCIÓN)
        // Este es el que consume tu pantalla de Admin para ver barras y porcentajes
        // --------------------------------------------------------------------
        [HttpGet("EnVivo/{eleccionId}")]
        public async Task<IActionResult> GetResultadosEnVivo(int eleccionId)
        {
            // A. Contamos votos totales para esa elección
            var totalVotos = await _context.VotosAnonimos
                .CountAsync(v => v.EleccionId == eleccionId);

            // B. Agrupamos los votos por Candidato (Cédula)
            var conteo = await _context.VotosAnonimos
                .Where(v => v.EleccionId == eleccionId)
                .GroupBy(v => v.CedulaCandidato)
                .Select(g => new
                {
                    Cedula = g.Key,
                    Votos = g.Count()
                })
                .ToListAsync();

            // C. Armamos la lista con nombres y detalles visuales
            var resultados = new List<object>();

            foreach (var item in conteo)
            {
                // Buscamos datos del candidato (Nombre, Foto)
                var datosVotante = await _context.Votantes
                    .AsNoTracking()
                    .Where(v => v.Cedula == item.Cedula)
                    .Select(v => new { v.NombreCompleto, v.FotoUrl })
                    .FirstOrDefaultAsync();

                // Buscamos datos de su Lista/Partido
                var datosCandidato = await _context.Candidatos
                    .Include(c => c.Lista)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Cedula == item.Cedula);

                double porcentaje = totalVotos > 0
                    ? (double)item.Votos / totalVotos * 100
                    : 0;

                resultados.Add(new
                {
                    Nombre = datosVotante?.NombreCompleto ?? "Desconocido",
                    FotoUrl = datosVotante?.FotoUrl,
                    Lista = datosCandidato?.Lista?.NombreLista ?? "Independiente",
                    ListaLogo = datosCandidato?.Lista?.LogoUrl,
                    Votos = item.Votos,
                    Porcentaje = Math.Round(porcentaje, 2)
                });
            }

            // Ordenamos: El que tenga más votos primero (Ganador)
            resultados = resultados.OrderByDescending(r => ((dynamic)r).Votos).ToList();

            return Ok(new
            {
                EleccionId = eleccionId,
                TotalVotos = totalVotos,
                Detalle = resultados
            });
        }

        // --------------------------------------------------------------------
        // 2. VALIDACIÓN PARA CIERRE DE JUNTA
        // Compara el Padrón vs Los que tienen marca "HaVotado"
        // --------------------------------------------------------------------
        [HttpGet("ValidarCierreJunta/{juntaId}")]
        public async Task<IActionResult> ValidarCierreJunta(int juntaId)
        {
            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null) return NotFound("Junta no encontrada");

            // Total de personas empadronadas en esa mesa
            var totalPadron = await _context.Votantes
                .CountAsync(v => v.JuntaId == juntaId);

            // Total de personas que YA VOTARON (Campo HaVotado = true)
            // Esta es la forma correcta de contar participación en mesa
            var votosRecibidos = await _context.Votantes
                .CountAsync(v => v.JuntaId == juntaId && v.HaVotado);

            return Ok(new
            {
                JuntaId = junta.Id,
                Mesa = junta.NumeroMesa,
                TotalPadron = totalPadron,
                VotosRecibidos = votosRecibidos,
                Pendientes = totalPadron - votosRecibidos,
                Avance = totalPadron > 0 ? (votosRecibidos * 100 / totalPadron) : 0
            });
        }

        // --------------------------------------------------------------------
        // 3. AVANCE GENERAL DE PARTICIPACIÓN
        // Útil para dashboard del Admin
        // --------------------------------------------------------------------
        [HttpGet("AvanceGeneral")]
        public async Task<IActionResult> AvanceGeneral()
        {
            var totalVotantes = await _context.Votantes.CountAsync(v => v.RolId == 2 || v.RolId == 3); // Votantes y Jefes
            var totalVotos = await _context.Votantes.CountAsync(v => v.HaVotado);

            return Ok(new
            {
                TotalEsperado = totalVotantes,
                TotalVotos = totalVotos,
                Porcentaje = totalVotantes > 0 ? Math.Round((double)totalVotos / totalVotantes * 100, 2) : 0
            });
        }
    }
}