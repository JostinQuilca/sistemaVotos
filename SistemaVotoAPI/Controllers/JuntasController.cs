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
    // [Authorize(Roles = "1")] // Por defecto, solo el Administrador tiene acceso
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
        // Devuelve las juntas con formato legible para las vistas de administración
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
                    Ubicacion = j.Direccion != null
                        ? $"{j.Direccion.Provincia} - {j.Direccion.Canton} - {j.Direccion.Parroquia}"
                        : "Sin dirección",

                    NombreJefe = string.IsNullOrEmpty(j.JefeDeJuntaId)
                        ? "Sin asignar"
                        : (j.JefeDeJunta != null ? j.JefeDeJunta.NombreCompleto : "Usuario no encontrado"),

                    EstadoJunta = j.Estado
                })
                .OrderBy(j => j.Id)
                .ToListAsync();

            return Ok(juntas);
        }

        // =========================================================================
        // ⭐ NUEVA FUNCIÓN AÑADIDA: OBTENER JUNTA INDIVIDUAL (NECESARIA PARA EL JEFE)
        // =========================================================================
        // GET: api/Juntas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<JuntaDetalleDto>> GetJunta(long id)
        {
            var junta = await _context.Juntas
                .Include(j => j.Direccion)
                .Include(j => j.JefeDeJunta)
                .Where(j => j.Id == id)
                .Select(j => new JuntaDetalleDto
                {
                    Id = j.Id,
                    NumeroMesa = j.NumeroMesa,
                    Ubicacion = j.Direccion != null
                        ? $"{j.Direccion.Provincia} - {j.Direccion.Canton} - {j.Direccion.Parroquia}"
                        : "Sin dirección",
                    NombreJefe = string.IsNullOrEmpty(j.JefeDeJuntaId)
                        ? "Sin asignar"
                        : (j.JefeDeJunta != null ? j.JefeDeJunta.NombreCompleto : "Usuario no encontrado"),
                    EstadoJunta = j.Estado
                })
                .FirstOrDefaultAsync();

            if (junta == null)
            {
                return NotFound();
            }

            return Ok(junta);
        }

        // GET: api/Juntas/PorEleccion/{id}
        [HttpGet("PorEleccion/{eleccionId}")]
        public async Task<ActionResult<IEnumerable<JuntaDetalleDto>>> GetJuntasPorEleccion(int eleccionId)
        {
            var juntas = await _context.Juntas
                .Where(j => j.EleccionId == eleccionId)
                .Include(j => j.Direccion)
                .Include(j => j.JefeDeJunta)
                .Select(j => new JuntaDetalleDto
                {
                    Id = j.Id,
                    NumeroMesa = j.NumeroMesa,
                    Ubicacion = j.Direccion != null
                        ? $"{j.Direccion.Provincia} - {j.Direccion.Canton} - {j.Direccion.Parroquia}"
                        : "Sin dirección",

                    NombreJefe = string.IsNullOrEmpty(j.JefeDeJuntaId)
                        ? "Sin asignar"
                        : (j.JefeDeJunta != null ? j.JefeDeJunta.NombreCompleto : "Usuario no encontrado"),

                    EstadoJunta = j.Estado
                })
                .OrderBy(j => j.Id)
                .ToListAsync();

            return Ok(juntas);
        }

        // POST: api/Juntas/crear/{direccionId}/{cantidad}
        [HttpPost("CrearPorDireccion")]
        public async Task<IActionResult> CrearPorDireccion(
          [FromQuery] int direccionId,
          [FromQuery] int cantidad)
        {
            var direccion = await _context.Direcciones.FindAsync(direccionId);

            if (direccion == null)
                return NotFound("Dirección no encontrada.");

            if (cantidad <= 0)
                return BadRequest("La cantidad debe ser mayor a cero.");

            // ⭐ OBTENER ELECCIÓN ACTIVA
            var eleccionActiva = await _context.Elecciones
                .FirstOrDefaultAsync(e => e.Estado == "ACTIVA");

            if (eleccionActiva == null)
                return BadRequest("No existe una elección activa.");

            int juntasExistentes = await _context.Juntas
                .CountAsync(j => j.DireccionId == direccionId
                              && j.EleccionId == eleccionActiva.Id);

            var nuevasJuntas = new List<Junta>();

            for (int i = 1; i <= cantidad; i++)
            {
                int numeroMesa = juntasExistentes + i;

                nuevasJuntas.Add(new Junta
                {
                    NumeroMesa = numeroMesa,
                    DireccionId = direccionId,
                    Estado = 1,
                    JefeDeJuntaId = null,
                    EleccionId = eleccionActiva.Id
                });
            }

            _context.Juntas.AddRange(nuevasJuntas);
            await _context.SaveChangesAsync();

            return Ok("Mesas creadas correctamente.");
        }

        // PUT: api/Juntas/AsignarJefe
        [HttpPut("AsignarJefe")]
        public async Task<IActionResult> AsignarJefe(long juntaId, string cedulaVotante)
        {
            var junta = await _context.Juntas.FindAsync(juntaId);
            if (junta == null) return NotFound("Junta no encontrada");

            var votante = await _context.Votantes.FindAsync(cedulaVotante);
            if (votante == null) return NotFound("El votante no existe");

            // Regla de negocio: Candidatos no pueden ser autoridades de mesa
            bool esCandidato = await _context.Candidatos.AnyAsync(c => c.Cedula == cedulaVotante);
            if (esCandidato) return BadRequest("Un candidato no puede ser jefe de junta");

            // Asignación de responsabilidad
            junta.JefeDeJuntaId = cedulaVotante;

            // Al asignar un jefe, la mesa se considera lista para la jornada (2 = ABIERTA)
            if (junta.Estado == 1) junta.Estado = 2;

            // Actualización de perfil del usuario
            votante.RolId = 3; // Rol: Jefe de Junta
            votante.JuntaId = junta.Id;

            await _context.SaveChangesAsync();
            return Ok("Jefe de junta asignado correctamente y mesa habilitada.");
        }

        // DELETE: api/Juntas/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJunta(long id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null) return NotFound();

            // Si tenía un jefe asignado, le devolvemos su rol original de votante
            if (!string.IsNullOrEmpty(junta.JefeDeJuntaId))
            {
                var jefe = await _context.Votantes.FindAsync(junta.JefeDeJuntaId);
                if (jefe != null)
                {
                    jefe.RolId = 2; // Vuelve a ser Votante normal
                    jefe.JuntaId = null;
                }
            }

            _context.Juntas.Remove(junta);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Juntas/CerrarMesa/{id}
        // Acción realizada por el Jefe de Junta al finalizar la jornada
        [AllowAnonymous]
        [HttpPut("CerrarMesa/{id}")]
        public async Task<IActionResult> CerrarMesa(long id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null) return NotFound("Junta no encontrada");

            // Solo se puede cerrar lo que está abierto (2)
            if (junta.Estado != 2)
                return BadRequest($"La mesa no está en fase de votación (Estado actual: {junta.Estado})");

            junta.Estado = 3; // 3 = PENDIENTE (Espera de validación del Admin)
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Mesa cerrada correctamente. Los datos han sido enviados para validación." });
        }

        // PUT: api/Juntas/AprobarJunta/{id}
        // Acción realizada por el Administrador para oficializar el escrutinio
        [HttpPut("AprobarJunta/{id}")]
        public async Task<IActionResult> AprobarJunta(long id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null) return NotFound("Junta no encontrada");

            // El estado 4 significa que el Admin confirma que el proceso fue limpio
            junta.Estado = 4; // 4 = APROBADA/CONTEO OFICIAL
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Junta aprobada. Los votos se han integrado al conteo oficial." });
        }
        // =========================================================================
        // ⭐ NUEVO ENDPOINT: OBTENER JEFES DISPONIBLES (FILTRO REAL)
        // =========================================================================
        // GET: api/Juntas/PosiblesJefes
        [HttpGet("PosiblesJefes")]
        public async Task<ActionResult<IEnumerable<Votante>>> GetPosiblesJefes()
        {
            // Lógica:
            // 1. Buscamos usuarios con Rol 3 (Jefes de Junta).
            // 2. Filtramos para excluir a aquellos cuya Cédula YA ESTÁ registrada 
            //    en la columna 'JefeDeJuntaId' de la tabla 'Juntas'.

            var jefesDisponibles = await _context.Votantes
                .Where(v => v.RolId == 3 && !_context.Juntas.Any(j => j.JefeDeJuntaId == v.Cedula))
                .OrderBy(v => v.NombreCompleto)
                .ToListAsync();

            return Ok(jefesDisponibles);
        
        }
        // =========================================================
        // ⭐ NUEVO: INICIAR JORNADA (Forzar estado 1 -> 2)
        // =========================================================
        [HttpPut("IniciarJornada/{id}")]
        public async Task<IActionResult> IniciarJornada(long id)
        {
            var junta = await _context.Juntas.FindAsync(id);
            if (junta == null) return NotFound("Junta no encontrada");

            // Si ya está abierta, devolvemos éxito
            if (junta.Estado == 2) return Ok("La mesa ya estaba abierta.");

            // Solo permitimos pasar de CREADA (1) a ABIERTA (2)
            if (junta.Estado == 1)
            {
                junta.Estado = 2;
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Jornada iniciada correctamente." });
            }

            return BadRequest($"No se puede iniciar. Estado actual: {junta.Estado}");
        }
    }
}