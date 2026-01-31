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
            // Asumimos que el "Name" del usuario logueado es su Cédula (según configuración estándar de Identity)
            // O buscamos el claim específico si usas uno diferente.
            var cedulaJefe = User.Identity?.Name;

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

                // --- LÓGICA NUEVA PARA EL VOTO DEL JEFE ---
                bool jefeHaVotado = true; // Por defecto true para no mostrar botón si falla algo
                string nombreJefe = "Jefe de Mesa";

                if (votantes != null && !string.IsNullOrEmpty(cedulaJefe))
                {
                    // Buscamos al jefe en la lista de votantes de esta mesa
                    var datosJefe = votantes.FirstOrDefault(v => v.GetProperty("cedula").GetString() == cedulaJefe);

                    // Verificamos si existe (ValueKind no es Undefined)
                    if (datosJefe.ValueKind != JsonValueKind.Undefined)
                    {
                        jefeHaVotado = datosJefe.GetProperty("haVotado").GetBoolean();
                        nombreJefe = datosJefe.GetProperty("nombreCompleto").GetString();
                    }
                }

                // Pasamos estos datos a la vista
                ViewBag.CedulaJefe = cedulaJefe;
                ViewBag.NombreJefe = nombreJefe;
                ViewBag.JefeHaVotado = jefeHaVotado;

                // Ordenamos la lista
                var votantesOrdenados = votantes?
                    .OrderBy(v => v.GetProperty("haVotado").GetBoolean())
                    .ThenBy(v => v.GetProperty("nombreCompleto").GetString())
                    .ToList();

                return View(votantesOrdenados);
            }

            return View(new List<JsonElement>());
        }

        // ACCIÓN PARA GENERAR EL TOKEN (Llama a la API)
        [HttpPost]
        public async Task<IActionResult> GenerarToken(string cedula)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Llamamos al nuevo controlador TokensController que creamos arriba
            var response = await client.PostAsync($"api/Tokens/Generar/{cedula}", null);

            if (response.IsSuccessStatusCode)
            {
                var resultado = await response.Content.ReadFromJsonAsync<dynamic>();
                // Retornamos el token al JavaScript de la vista
                return Json(new { success = true, token = resultado.GetProperty("token").ToString() });
            }
            else
            {
                return Json(new { success = false, message = "Error al conectar con el servidor." });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CerrarMesa()
        {
            // Recuperamos el ID de la cookie
            var juntaClaim = User.FindFirst("JuntaId")?.Value;
            if (string.IsNullOrEmpty(juntaClaim)) return RedirectToAction("Login", "Aut");

            int juntaId = int.Parse(juntaClaim);
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Llamamos a la API para cambiar estado a PENDIENTE
            var response = await client.PutAsync($"api/Juntas/CerrarMesa/{juntaId}", null);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Mesa cerrada. Esperando confirmación del Admin." });
            }
            else
            {
                return Json(new { success = false, message = "Error al cerrar la mesa." });
            }
        }
        // ACCIÓN: INICIAR MESA (Cuando está en estado 1)
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