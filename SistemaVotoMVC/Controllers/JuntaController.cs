using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaVotoMVC.Controllers
{
    [Authorize(Roles = "3")] // Solo Jefes de Junta
    public class JuntaController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public JuntaController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // PANTALLA PRINCIPAL DE LA JUNTA
        public async Task<IActionResult> Index()
        {
            var juntaClaim = User.FindFirst("JuntaId")?.Value;

            // --- CORRECCIÓN AQUÍ ---
            // Usamos NameIdentifier que es donde guardamos la CÉDULA en el Login
            var cedulaJefe = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // -----------------------

            if (string.IsNullOrEmpty(juntaClaim) || juntaClaim == "0")
            {
                return RedirectToAction("Login", "Aut");
            }

            long juntaId = long.Parse(juntaClaim);
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 1. Obtener datos de la Junta
            var responseJunta = await client.GetAsync($"api/Juntas/{juntaId}");

            // 2. Obtener votantes de esta junta
            var responseVotantes = await client.GetAsync($"api/Votantes/PorJunta/{juntaId}");

            if (responseJunta.IsSuccessStatusCode && responseVotantes.IsSuccessStatusCode)
            {
                ViewBag.DatosJunta = await responseJunta.Content.ReadFromJsonAsync<JsonElement>();
                var votantes = await responseVotantes.Content.ReadFromJsonAsync<List<JsonElement>>();

                // --- LÓGICA PARA EL VOTO DEL JEFE ---
                bool jefeHaVotado = true; // Por defecto pesimista
                string nombreJefe = "Jefe de Mesa";

                if (votantes != null && !string.IsNullOrEmpty(cedulaJefe))
                {
                    // Ahora sí comparamos Cédula con Cédula
                    var datosJefe = votantes.FirstOrDefault(v => v.GetProperty("cedula").GetString() == cedulaJefe);

                    // Si lo encontramos en la lista, tomamos su estado real
                    if (datosJefe.ValueKind != JsonValueKind.Undefined)
                    {
                        jefeHaVotado = datosJefe.GetProperty("haVotado").GetBoolean();
                        nombreJefe = datosJefe.GetProperty("nombreCompleto").GetString();
                    }
                }

                ViewBag.CedulaJefe = cedulaJefe;
                ViewBag.NombreJefe = nombreJefe;
                ViewBag.JefeHaVotado = jefeHaVotado;

                // Ordenamos: Pendientes primero
                var votantesOrdenados = votantes?
                    .OrderBy(v => v.GetProperty("haVotado").GetBoolean())
                    .ThenBy(v => v.GetProperty("nombreCompleto").GetString())
                    .ToList();

                return View(votantesOrdenados);
            }

            return View(new List<JsonElement>());
        }

        // ACCIÓN PARA GENERAR EL TOKEN
        [HttpPost]
        public async Task<IActionResult> GenerarToken(string cedula)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsync($"api/Tokens/Generar/{cedula}", null);

            if (response.IsSuccessStatusCode)
            {
                var resultado = await response.Content.ReadFromJsonAsync<JsonElement>();
                return Json(new { success = true, token = resultado.GetProperty("token").ToString() });
            }
            else
            {
                return Json(new { success = false, message = "Error al generar token." });
            }
        }

        // CERRAR MESA
        [HttpPost]
        public async Task<IActionResult> CerrarMesa()
        {
            var juntaClaim = User.FindFirst("JuntaId")?.Value;
            if (string.IsNullOrEmpty(juntaClaim)) return Json(new { success = false, message = "Sesión inválida" });

            long juntaId = long.Parse(juntaClaim);
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var response = await client.PutAsync($"api/Juntas/CerrarMesa/{juntaId}", null);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Mesa cerrada correctamente." });
            }
            else
            {
                return Json(new { success = false, message = "Error al cerrar la mesa." });
            }
        }

        // INICIAR MESA
        [HttpPost]
        public async Task<IActionResult> IniciarMesa()
        {
            var juntaClaim = User.FindFirst("JuntaId")?.Value;
            if (string.IsNullOrEmpty(juntaClaim)) return Json(new { success = false, message = "Sesión inválida" });

            long juntaId = long.Parse(juntaClaim);
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var response = await client.PutAsync($"api/Juntas/IniciarJornada/{juntaId}", null);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Mesa abierta exitosamente." });
            }
            else
            {
                return Json(new { success = false, message = "Error al abrir mesa." });
            }
        }
    }
}