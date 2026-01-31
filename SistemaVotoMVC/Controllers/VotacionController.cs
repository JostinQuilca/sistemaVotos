using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text; // Necesario para StringContent
using System.Text.Json;

namespace SistemaVotoMVC.Controllers
{
    [Authorize(Roles = "2")]
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

        // PANTALLA 1: Validación de Mesa y Lista de Elecciones
        public async Task<IActionResult> Index()
        {
            // 1. Obtener Cédula
            var cedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(cedula)) return RedirectToAction("Login", "Aut");

            // 2. Verificar si ya votó
            if (await YaVoto(cedula)) return RedirectToAction("Certificado");

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 3. OBTENER DATOS DEL VOTANTE (Para saber su Junta)
            var responseVotante = await client.GetAsync($"{_endpointVotantes}/{cedula}");
            if (!responseVotante.IsSuccessStatusCode)
            {
                ViewBag.Mensaje = "No se pudo cargar su información de votante.";
                return View("MesaNoDisponible");
            }
            var votante = await responseVotante.Content.ReadFromJsonAsync<Votante>();

            // 4. VERIFICAR ASIGNACIÓN DE JUNTA
            if (votante.JuntaId == null || votante.JuntaId <= 0)
            {
                ViewBag.Mensaje = "Usted no se encuentra empadronado en ninguna mesa.";
                ViewBag.DebugInfo = "JuntaId es nulo o 0 en la base de datos.";
                return View("MesaNoDisponible");
            }

            // 5. VERIFICAR ESTADO DE LA JUNTA (CRÍTICO)
            var responseJunta = await client.GetAsync($"{_endpointJuntas}/{votante.JuntaId}");
            if (responseJunta.IsSuccessStatusCode)
            {
                // Usamos JsonElement para ser flexibles con el formato de respuesta
                var juntaJson = await responseJunta.Content.ReadFromJsonAsync<JsonElement>();

                // Intentamos obtener el estado (puede venir como 'estado' o 'estadoJunta' según tu DTO)
                int estado = 0;
                if (juntaJson.TryGetProperty("estadoJunta", out var propEstado)) estado = propEstado.GetInt32();
                else if (juntaJson.TryGetProperty("estado", out var propEstadoOld)) estado = propEstadoOld.GetInt32();

                // Intentamos obtener ID y Numero para el debug
                long jId = 0; int jNum = 0;
                if (juntaJson.TryGetProperty("id", out var pId)) jId = pId.GetInt64();
                if (juntaJson.TryGetProperty("numeroMesa", out var pNum)) jNum = pNum.GetInt32();

                // SI EL ESTADO NO ES 2 (ABIERTA), BLOQUEAMOS EL ACCESO
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
                    ViewBag.DebugInfo = $"Mesa ID: {jId} | Número: {jNum} | Estado Actual: {estado} (Se requiere 2)";
                    return View("MesaNoDisponible");
                }
            }
            else
            {
                ViewBag.Mensaje = "Error al conectar con la Mesa Electoral.";
                return View("MesaNoDisponible");
            }

            // 6. SI TODO ESTÁ BIEN, CARGAMOS LAS ELECCIONES
            var responseEle = await client.GetAsync(_endpointElecciones);
            var elecciones = responseEle.IsSuccessStatusCode
                ? await responseEle.Content.ReadFromJsonAsync<List<Eleccion>>()
                : new List<Eleccion>();

            var activas = elecciones?.Where(e => e.Estado == "ACTIVA").ToList() ?? new List<Eleccion>();
            return View(activas);
        }

        // PANTALLA 2: Papeleta
        [HttpGet]
        public async Task<IActionResult> Papeleta(int eleccionId)
        {
            var cedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (await YaVoto(cedula)) return RedirectToAction("Certificado");

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
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

        // ACCIÓN: Guardar el Voto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmitirVoto(int eleccionId, string cedulaCandidato)
        {
            var miCedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Verificar ANTES de intentar nada
            if (await YaVoto(miCedula)) return RedirectToAction("Certificado");

            var voto = new VotoAnonimo
            {
                EleccionId = eleccionId,
                CedulaCandidato = cedulaCandidato,
                FechaVoto = DateTime.Now
            };

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 2. Registrar el Voto Anónimo
            var responseVoto = await client.PostAsJsonAsync(_endpointVotos, voto);

            if (!responseVoto.IsSuccessStatusCode)
            {
                TempData["Error"] = "Error al conectar con la urna digital.";
                return RedirectToAction("Papeleta", new { eleccionId });
            }

            // 3. CRÍTICO: Marcar que YA VOTÓ
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var responseMarca = await client.PutAsync($"{_endpointAut}/MarcarVoto/{miCedula}", content);

            if (responseMarca.IsSuccessStatusCode)
            {
                return RedirectToAction("Certificado");
            }
            else
            {
                TempData["Error"] = "El voto se recibió, pero hubo un error al actualizar su estado. Contacte al administrador.";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Certificado()
        {
            return View();
        }

        // MÉTODO AUXILIAR
        private async Task<bool> YaVoto(string cedula)
        {
            if (string.IsNullOrEmpty(cedula)) return true;

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync($"{_endpointAut}/VerificarEstado/{cedula}");

            if (response.IsSuccessStatusCode)
            {
                var haVotado = await response.Content.ReadFromJsonAsync<bool>();
                return haVotado;
            }

            return true; // Fail-Closed
        }
    }
}