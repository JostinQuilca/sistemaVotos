using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaVotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EleccionesController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public EleccionesController(APIVotosDbContext context)
        {
            _context = context;
        }

        // Helpers
        private static string CalcularEstado(Eleccion e, DateTime now)
        {
            if (now < e.FechaInicio) return "CONFIGURACION";
            if (now >= e.FechaInicio && now < e.FechaFin) return "ACTIVA";
            return "FINALIZADA";
        }

        // GET: api/Elecciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Eleccion>>> GetEleccion()
        {
            var now = DateTime.Now;
            var elecciones = await _context.Elecciones.ToListAsync();

            bool huboCambios = false;
            foreach (var e in elecciones)
            {
                var nuevoEstado = CalcularEstado(e, now);
                if (e.Estado != nuevoEstado)
                {
                    e.Estado = nuevoEstado;
                    huboCambios = true;
                }
            }

            if (huboCambios)
                await _context.SaveChangesAsync();

            return elecciones;
        }

        // GET: api/Elecciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Eleccion>> GetEleccion(int id)
        {
            var eleccion = await _context.Elecciones.FindAsync(id);
            if (eleccion == null) return NotFound();

            var now = DateTime.Now;
            var nuevoEstado = CalcularEstado(eleccion, now);

            if (eleccion.Estado != nuevoEstado)
            {
                eleccion.Estado = nuevoEstado;
                await _context.SaveChangesAsync();
            }

            return eleccion;
        }

        // POST: api/Elecciones
        [HttpPost]
        public async Task<ActionResult<Eleccion>> PostEleccion(Eleccion eleccion)
        {
            if (eleccion == null) return BadRequest("Datos no proporcionados.");

            eleccion.Titulo = (eleccion.Titulo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(eleccion.Titulo))
                return BadRequest("El título es obligatorio.");

            if (eleccion.FechaFin <= eleccion.FechaInicio)
                return BadRequest("La fecha/hora fin debe ser mayor que la fecha/hora inicio.");

            // Estado siempre lo define el sistema
            eleccion.Estado = "CONFIGURACION";

            _context.Elecciones.Add(eleccion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEleccion), new { id = eleccion.Id }, eleccion);
        }

        // PUT: api/Elecciones/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEleccion(int id, Eleccion eleccion)
        {
            if (eleccion == null) return BadRequest("Datos no proporcionados.");
            if (id != eleccion.Id) return BadRequest("Id no coincide.");

            var existente = await _context.Elecciones.FindAsync(id);
            if (existente == null) return NotFound();

            var titulo = (eleccion.Titulo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(titulo))
                return BadRequest("El título es obligatorio.");

            if (eleccion.FechaFin <= eleccion.FechaInicio)
                return BadRequest("La fecha/hora fin debe ser mayor que la fecha/hora inicio.");

            existente.Titulo = titulo;
            existente.FechaInicio = eleccion.FechaInicio;
            existente.FechaFin = eleccion.FechaFin;

            // Estado se recalcula solo
            existente.Estado = CalcularEstado(existente, DateTime.Now);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Elecciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEleccion(int id)
        {
            var eleccion = await _context.Elecciones.FindAsync(id);
            if (eleccion == null) return NotFound();

            _context.Elecciones.Remove(eleccion);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

