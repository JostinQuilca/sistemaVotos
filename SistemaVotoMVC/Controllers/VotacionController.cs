using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text; // Necesario para StringContent

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

        public VotacionController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // PANTALLA 1: Lista de Elecciones
        public async Task<IActionResult> Index()
        {
            // Seguridad al entrar
            var cedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (await YaVoto(cedula)) return RedirectToAction("Certificado");

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync(_endpointElecciones);
            var elecciones = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Eleccion>>()
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
            // Usamos StringContent para asegurar que la petición PUT viaja correctamente
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var responseMarca = await client.PutAsync($"{_endpointAut}/MarcarVoto/{miCedula}", content);

            if (responseMarca.IsSuccessStatusCode)
            {
                // Solo si AMBOS tuvieron éxito, mostramos certificado
                return RedirectToAction("Certificado");
            }
            else
            {
                // Si el voto se guardó pero la marca falló, es un error grave de inconsistencia.
                // En un sistema real, haríamos rollback. Aquí mostramos error y forzamos salida.
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
            if (string.IsNullOrEmpty(cedula)) return true; // Si no hay cédula, bloqueamos por seguridad

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync($"{_endpointAut}/VerificarEstado/{cedula}");

            if (response.IsSuccessStatusCode)
            {
                var haVotado = await response.Content.ReadFromJsonAsync<bool>();
                return haVotado;
            }

            // Si la API falla o no responde, ASUMIMOS QUE SÍ VOTÓ para evitar fraude por error de sistema
            // (Fail-Closed Security)
            return true;
        }
    }
}