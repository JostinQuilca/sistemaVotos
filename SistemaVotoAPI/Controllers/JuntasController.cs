using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;

namespace SistemaVotoAPI.Controllers
{
    [Authorize(Roles = "1")]
    [Route("api/[controller]")]
    [ApiController]
    public class JuntasController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public JuntasController(APIVotosDbContext context)
        {
            _context = context;
        }

        // Devuelvo las juntas mostrando la dirección en texto y el nombre completo del jefe si existe
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JuntaDetalleDto>>> GetJuntas()
        {
            var juntas = await _context.Juntas
                .Include(j => j.Direccion)
                .Include(j => j.JefeDeJunta)
                .Select(j => new JuntaDetalleDto
                {
                    Id = j.Id,
                    NumeroMesa = j.NumeroMesa,
                    Ubicacion = $"{j.Direccion.Provincia} - {j.Direccion.Canton} - {j.Direccion.Parroquia}",
                    NombreJefe = j.JefeDeJuntaId == string.Empty
                        ? "Sin asignar"
                        : j.JefeDeJunta.NombreCompleto,
                    EstadoJunta = j.Estado
                })
                .OrderBy(j => j.Id)
                .ToListAsync();

            return Ok(juntas);
        }

        // Creo juntas automáticamente para una dirección existente, sin asignar jefe
        [HttpPost("CrearPorDireccion")]
        public async Task<IActionResult> CrearPorDireccion(int direccionId, int cantidad)
        {
            var direccion = await _context.Direcciones.FindAsync(direccionId);
            if (direccion == null)
                return BadRequest("La dirección no existe");

            if (cantidad <= 0)
                return BadRequest("La cantidad debe ser mayor a cero");

            int mesasExistentes = await _context.Juntas
                .CountAsync(j => j.DireccionId == direccionId);

            var nuevasJuntas = new List<Junta>();

            for (int i = 1; i <= cantidad; i++)
            {
                int numeroMesa = mesasExistentes + i;
                int juntaId = int.Parse($"{direccion.Id}{numeroMesa:D2}");

                nuevasJuntas.Add(new Junta
                {
                    Id = juntaId,
                    NumeroMesa = numeroMesa,
                    DireccionId = direccion.Id,
                    JefeDeJuntaId = string.Empty,
                    Estado = 1
                });
            }

            _context.Juntas.AddRange(nuevasJuntas);
            await _context.SaveChangesAsync();

            return Ok("Juntas creadas correctamente");
        }

        // Asigno un jefe de junta usando únicamente la cédula del votante
        [HttpPut("AsignarJefe")]
        public async Task<IActionResult> AsignarJefe(int juntaId, string cedulaVotante)
        {
            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null)
                return NotFound("Junta no encontrada");

            var votante = await _context.Votantes.FindAsync(cedulaVotante);
            if (votante == null)
                return NotFound("El votante no existe");

            bool esCandidato = await _context.Candidatos
                .AnyAsync(c => c.Cedula == cedulaVotante);

            if (esCandidato)
                return BadRequest("Un candidato no puede ser jefe de junta");

            junta.JefeDeJuntaId = cedulaVotante;
            votante.RolId = 3;

            await _context.SaveChangesAsync();
            return Ok("Jefe de junta asignado correctamente");
        }

        // Elimino juntas solo por control administrativo
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJunta(int id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null)
                return NotFound();

            _context.Juntas.Remove(junta);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
