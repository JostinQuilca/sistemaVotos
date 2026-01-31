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
    [Authorize(Roles = "1")] // Por defecto, solo el Administrador entra aquí
    [Route("api/[controller]")]
    [ApiController]
    public class JuntasController : ControllerBase
    {
        private readonly APIVotosDbContext _context;

        public JuntasController(APIVotosDbContext context)
        {
            _context = context;
        }

        // GET: api/Juntas
        // Devuelve las juntas con formato legible para la vista
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JuntaDetalleDto>>> GetJuntas()
        {
            var juntas = await _context.Juntas
                .Include(j => j.Direccion)
                .Include(j => j.JefeDeJunta) // Asegúrate de incluir la relación
                .Select(j => new JuntaDetalleDto
                {
                    Id = j.Id,
                    NumeroMesa = j.NumeroMesa,
                    // Construimos la ubicación concatenando campos
                    Ubicacion = j.Direccion != null
                        ? $"{j.Direccion.Provincia} - {j.Direccion.Canton} - {j.Direccion.Parroquia}"
                        : "Sin dirección",

                    // Verificación segura de nulos para el nombre del jefe
                    NombreJefe = string.IsNullOrEmpty(j.JefeDeJuntaId)
                        ? "Sin asignar"
                        : (j.JefeDeJunta != null ? j.JefeDeJunta.NombreCompleto : "Usuario no encontrado"),

                    EstadoJunta = j.Estado // Pasamos el int directo
                })
                .OrderBy(j => j.Id)
                .ToListAsync();

            return Ok(juntas);
        }

        // GET: api/Juntas/PorEleccion/5
        // (Opcional) Endpoint para filtrar si lo necesitas en 'VerificarJuntas'
        [HttpGet("PorEleccion/{eleccionId}")]
        public async Task<ActionResult<IEnumerable<JuntaDetalleDto>>> GetJuntasPorEleccion(int eleccionId)
        {
            // Nota: Si tus juntas no están ligadas a elección en BD, devolvemos todas.
            return await GetJuntas();
        }

        // POST: api/Juntas/CrearPorDireccion
        [HttpPost("CrearPorDireccion")]
        public async Task<IActionResult> CrearPorDireccion(int direccionId, int cantidad)
        {
            var direccion = await _context.Direcciones.FindAsync(direccionId);
            if (direccion == null) return BadRequest("La dirección no existe");

            if (cantidad <= 0) return BadRequest("La cantidad debe ser mayor a cero");

            int mesasExistentes = await _context.Juntas
                .CountAsync(j => j.DireccionId == direccionId);

            var nuevasJuntas = new List<Junta>();

            for (int i = 1; i <= cantidad; i++)
            {
                int numeroMesa = mesasExistentes + i;
                // Generamos un ID compuesto (ej: Direccion 10 + Mesa 1 = 1001)
                // Usamos long.Parse si los IDs son muy grandes, o int.Parse si caben.
                int juntaId = int.Parse($"{direccion.Id}{numeroMesa:D2}");

                nuevasJuntas.Add(new Junta
                {
                    Id = juntaId,
                    NumeroMesa = numeroMesa,
                    DireccionId = direccion.Id,
                    JefeDeJuntaId = null, // Usamos null en lugar de string.Empty
                    Estado = 1 // 1 = CERRADA (Inicial)
                });
            }

            _context.Juntas.AddRange(nuevasJuntas);
            await _context.SaveChangesAsync();

            return Ok("Juntas creadas correctamente");
        }

        // PUT: api/Juntas/AsignarJefe
        [HttpPut("AsignarJefe")]
        public async Task<IActionResult> AsignarJefe(int juntaId, string cedulaVotante)
        {
            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null) return NotFound("Junta no encontrada");

            var votante = await _context.Votantes.FindAsync(cedulaVotante);
            if (votante == null) return NotFound("El votante no existe");

            // Verificar que no sea candidato
            bool esCandidato = await _context.Candidatos.AnyAsync(c => c.Cedula == cedulaVotante);
            if (esCandidato) return BadRequest("Un candidato no puede ser jefe de junta");

            // Asignación
            junta.JefeDeJuntaId = cedulaVotante;

            // Si la mesa estaba en estado 1 (Cerrada/Inicial), la pasamos a 2 (Abierta/Lista)
            if (junta.Estado == 1) junta.Estado = 2;

            // Actualizamos rol del votante
            votante.RolId = 3;
            votante.JuntaId = junta.Id; // Vinculamos al votante con su mesa

            await _context.SaveChangesAsync();
            return Ok("Jefe de junta asignado correctamente");
        }

        // DELETE: api/Juntas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJunta(int id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null) return NotFound();

            // Opcional: Desvincular al jefe antes de borrar
            if (!string.IsNullOrEmpty(junta.JefeDeJuntaId))
            {
                var jefe = await _context.Votantes.FindAsync(junta.JefeDeJuntaId);
                if (jefe != null)
                {
                    jefe.RolId = 2; // Vuelve a ser votante normal
                    jefe.JuntaId = null;
                }
            }

            _context.Juntas.Remove(junta);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Juntas/CerrarMesa/5
        // Permite acceso a Roles 1 (Admin) y 3 (Jefe Junta) o Anónimo si el control es por MVC
        [AllowAnonymous]
        [HttpPut("CerrarMesa/{id}")]
        public async Task<IActionResult> CerrarMesa(int id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null) return NotFound("Junta no encontrada");

            // Validamos que el estado sea 2 (ABIERTA)
            if (junta.Estado != 2)
                return BadRequest($"La mesa no está abierta para cierre (Estado actual: {junta.Estado})");

            junta.Estado = 3; // 3 = PENDIENTE
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Mesa cerrada correctamente." });
        }

        // PUT: api/Juntas/AprobarJunta/5
        // Este se mantiene solo para Admin (hereda el [Authorize] de la clase)
        [HttpPut("AprobarJunta/{id}")]
        public async Task<IActionResult> AprobarJunta(int id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null) return NotFound("Junta no encontrada");

            junta.Estado = 4; // 4 = APROBADA/CONTEO
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Junta aprobada." });
        }
    }
}