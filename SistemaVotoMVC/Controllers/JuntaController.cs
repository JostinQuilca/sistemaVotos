using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Http.Json;

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
            // Recuperamos el ID de la Junta desde la cookie
            var juntaClaim = User.FindFirst("JuntaId")?.Value;
            if (string.IsNullOrEmpty(juntaClaim) || juntaClaim == "0")
            {
                return RedirectToAction("Login", "Aut");
            }

            int juntaId = int.Parse(juntaClaim);
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Llamamos a la API para obtener datos de la junta y sus votantes
            var responseJunta = await client.GetAsync($"api/Juntas/{juntaId}");
            var responseVotantes = await client.GetAsync($"api/Votantes/PorJunta/{juntaId}");

            if (responseJunta.IsSuccessStatusCode && responseVotantes.IsSuccessStatusCode)
            {
                ViewBag.DatosJunta = await responseJunta.Content.ReadFromJsonAsync<object>();
                var votantes = await responseVotantes.Content.ReadFromJsonAsync<List<object>>();
                return View(votantes);
            }

            // Si falla, mostramos lista vacía
            return View(new List<object>());
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
    }
}