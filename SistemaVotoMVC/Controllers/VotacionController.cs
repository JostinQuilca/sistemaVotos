using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text; // Necesario para StringContent
using System.Text.Json;
using SistemaVotoMVC.Helpers; // Necesario para el helper de correo

namespace SistemaVotoMVC.Controllers
{
    // Permitimos Votantes (2), Jefes (3) y Administradores (1)
    [Authorize(Roles = "1, 2, 3")]
    public class VotacionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // Endpoints
        private readonly string _endpointElecciones = "api/Elecciones";
        private readonly string _endpointCandidatos = "api/Candidatos";
        private readonly string _endpointVotos = "api/VotosAnonimos";
        private readonly string _endpointAut = "api/Aut";
        private readonly string _endpointVotantes = "api/Votantes";
        private readonly string _endpointJuntas = "api/Juntas";

        public VotacionController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // -------------------------------------------------------------------
        // PANTALLA 1: LISTA DE ELECCIONES (INDEX)
        // -------------------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            // 1. Obtener Cédula
            var cedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(cedula)) return RedirectToAction("Login", "Aut");

            // 2. Verificar si ya votó
            if (await YaVoto(cedula)) return RedirectToAction("Certificado");

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 3. Obtener datos del Votante
            var responseVotante = await client.GetAsync($"{_endpointVotantes}/{cedula}");
            if (!responseVotante.IsSuccessStatusCode)
            {
                ViewBag.Mensaje = "No se pudo cargar su perfil de votante.";
                return View("MesaNoDisponible");
            }
            var votante = await responseVotante.Content.ReadFromJsonAsync<Votante>();

            // ---------------------------------------------------------
            // VALIDACIÓN ESTRICTA DE MESA (PARA TODOS: ADMIN, JEFE Y VOTANTE)
            // ---------------------------------------------------------

            // A) Verificar Asignación de Mesa
            if (votante.JuntaId == null || votante.JuntaId <= 0)
            {
                ViewBag.Mensaje = "Usted no se encuentra empadronado en ninguna mesa.";
                ViewBag.DebugInfo = "Su usuario requiere estar asignado a una Junta para votar.";
                return View("MesaNoDisponible");
            }

            // B) Verificar Estado de la Mesa
            var responseJunta = await client.GetAsync($"{_endpointJuntas}/{votante.JuntaId}");
            if (responseJunta.IsSuccessStatusCode)
            {
                var juntaJson = await responseJunta.Content.ReadFromJsonAsync<JsonElement>();

                int estado = 0;
                if (juntaJson.TryGetProperty("estadoJunta", out var p)) estado = p.GetInt32();
                else if (juntaJson.TryGetProperty("estado", out var pOld)) estado = pOld.GetInt32();

                long jId = 0;
                if (juntaJson.TryGetProperty("id", out var pId)) jId = pId.GetInt64();

                // SI NO ES 2 (ABIERTA), BLOQUEAMOS EL ACCESO
                if (estado != 2)
                {
                    string motivo = "";
                    switch (estado)
                    {
                        case 1: motivo = "SU MESA AÚN NO HA SIDO ABIERTA POR EL JEFE DE JUNTA."; break;
                        case 3: motivo = "LA VOTACIÓN EN ESTA MESA HA FINALIZADO (Pendiente)."; break;
                        case 4: motivo = "LA JORNADA ELECTORAL HA CONCLUIDO."; break;
                        default: motivo = "MESA NO DISPONIBLE."; break;
                    }

                    ViewBag.Mensaje = motivo;
                    ViewBag.DebugInfo = $"Mesa ID: {jId} | Estado Actual: {estado} (Se requiere estado 2)";
                    return View("MesaNoDisponible");
                }
            }
            else
            {
                ViewBag.Mensaje = "Error al conectar con la Mesa Electoral.";
                return View("MesaNoDisponible");
            }
            // ---------------------------------------------------------

            // 4. Cargar Elecciones Activas
            var responseEle = await client.GetAsync(_endpointElecciones);
            var elecciones = responseEle.IsSuccessStatusCode
                ? await responseEle.Content.ReadFromJsonAsync<List<Eleccion>>()
                : new List<Eleccion>();

            var activas = elecciones?.Where(e => e.Estado == "ACTIVA").ToList() ?? new List<Eleccion>();
            return View(activas);
        }

        // -------------------------------------------------------------------
        // PANTALLA 2: PAPELETA DE VOTACIÓN
        // -------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Papeleta(int eleccionId)
        {
            var cedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (await YaVoto(cedula)) return RedirectToAction("Certificado");

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // --- DATOS DE MESA Y RECINTO ---
            var respVotante = await client.GetAsync($"{_endpointVotantes}/{cedula}");
            if (respVotante.IsSuccessStatusCode)
            {
                var votante = await respVotante.Content.ReadFromJsonAsync<Votante>();

                // Como ya pasó la validación del Index, sabemos que tiene JuntaId válido
                if (votante.JuntaId != null && votante.JuntaId > 0)
                {
                    var respJunta = await client.GetAsync($"{_endpointJuntas}/{votante.JuntaId}");
                    if (respJunta.IsSuccessStatusCode)
                    {
                        var junta = await respJunta.Content.ReadFromJsonAsync<JsonElement>();

                        ViewBag.NumeroMesa = junta.TryGetProperty("numeroMesa", out var n) ? n.GetInt32() : 0;

                        if (junta.TryGetProperty("ubicacion", out var u))
                            ViewBag.Recinto = u.GetString();
                        else if (junta.TryGetProperty("direccion", out var d))
                            ViewBag.Recinto = $"{d.GetProperty("provincia")} - {d.GetProperty("canton")}";
                    }
                }
            }
            // -------------------------------

            var respEle = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            if (!respEle.IsSuccessStatusCode) return RedirectToAction("Index");

            var eleccion = await respEle.Content.ReadFromJsonAsync<Eleccion>();
            var respCand = await client.GetAsync($"{_endpointCandidatos}/PorEleccion/{eleccionId}");
            var candidatos = respCand.IsSuccessStatusCode
                ? await respCand.Content.ReadFromJsonAsync<List<Candidato>>()
                : new List<Candidato>();

            ViewBag.Eleccion = eleccion;
            return View(candidatos);
        }

        // -------------------------------------------------------------------
        // ACCIÓN: EMITIR VOTO (CON ENVÍO DE CORREO)
        // -------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmitirVoto(int eleccionId, string cedulaCandidato, string rolPostulante, int listaId)
        {
            var miCedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (await YaVoto(miCedula)) return RedirectToAction("Certificado");

            var voto = new VotoAnonimo
            {
                EleccionId = eleccionId,
                CedulaCandidato = cedulaCandidato,
                RolPostulante = rolPostulante, // VITAL
                ListaId = listaId,             // VITAL
                FechaVoto = DateTime.Now
            };

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 1. Enviar voto a la urna
            var responseVoto = await client.PostAsJsonAsync(_endpointVotos, voto);

            if (!responseVoto.IsSuccessStatusCode)
            {
                // Leer el error real
                var errorReal = await responseVoto.Content.ReadAsStringAsync();
                errorReal = errorReal.Trim('"');
                if (string.IsNullOrEmpty(errorReal)) errorReal = "Error desconocido al procesar su voto.";

                TempData["Error"] = $"No se pudo registrar su voto: {errorReal}";
                return RedirectToAction("Papeleta", new { eleccionId });
            }

            // 2. Marcar que ya votó
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var responseMarca = await client.PutAsync($"{_endpointAut}/MarcarVoto/{miCedula}", content);

            if (responseMarca.IsSuccessStatusCode)
            {
                // --- 3. ENVÍO DE CORREO (SEGUNDO PLANO) ---
                var respDatos = await client.GetAsync($"{_endpointVotantes}/{miCedula}");
                if (respDatos.IsSuccessStatusCode)
                {
                    var datosVotante = await respDatos.Content.ReadFromJsonAsync<Votante>();

                    // AQUÍ ESTÁ LA CORRECCIÓN: Se envían los 4 parámetros (Email, Nombre, Cédula, JuntaId)
                    _ = Task.Run(() => CorreoHelper.EnviarCertificado(
                            datosVotante.Email,
                            datosVotante.NombreCompleto,
                            datosVotante.Cedula,
                            datosVotante.JuntaId
                        ));
                }
                // ------------------------------------------

                return RedirectToAction("Certificado");
            }
            else
            {
                TempData["Error"] = "Voto recibido, pero error al actualizar estado.";
                return RedirectToAction("Index");
            }
        }

        // -------------------------------------------------------------------
        // PANTALLA: CERTIFICADO
        // -------------------------------------------------------------------
        public async Task<IActionResult> Certificado()
        {
            // Recuperamos datos para mostrar en el diploma (Vista PDF)
            var cedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var resp = await client.GetAsync($"{_endpointVotantes}/{cedula}");

            if (resp.IsSuccessStatusCode)
            {
                var votante = await resp.Content.ReadFromJsonAsync<Votante>();
                return View(votante); // Pasamos el objeto para pintar el nombre
            }

            return View(new Votante { NombreCompleto = "Ciudadano", Cedula = cedula });
        }

        private async Task<bool> YaVoto(string cedula)
        {
            if (string.IsNullOrEmpty(cedula)) return true;
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync($"{_endpointAut}/VerificarEstado/{cedula}");
            if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<bool>();
            return true;
        }
    }
}