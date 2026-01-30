using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Http.Json;

namespace SistemaVotoMVC.Controllers
{
    [Authorize(Roles = "3")]
    public class JuntaController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public JuntaController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            // --- SECCIÓN: RECUPERACIÓN DE CONTEXTO ---
            // Extraemos el ID de la junta que guardamos en la cookie durante el Login
            var juntaClaim = User.FindFirst("JuntaId")?.Value;

            if (string.IsNullOrEmpty(juntaClaim) || juntaClaim == "0")
            {
                return RedirectToAction("Login", "Aut");
            }

            int juntaId = int.Parse(juntaClaim);
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 1. Pedimos los detalles de la Junta (Dirección, Mesa, etc.)
            var responseJunta = await client.GetAsync($"api/Juntas/{juntaId}");

            // 2. Pedimos la lista de votantes que pertenecen a esta junta específica
            // Usamos el endpoint que ya tenemosguarado en VotantesController
            var responseVotantes = await client.GetAsync($"api/Votantes/Junta/{juntaId}");

            if (responseJunta.IsSuccessStatusCode && responseVotantes.IsSuccessStatusCode)
            {
                // Guardamos los datos de la junta en el ViewBag para mostrar el encabezado
                ViewBag.DatosJunta = await responseJunta.Content.ReadFromJsonAsync<object>();

                // Pasamos la lista de votantes como el modelo de la vista
                var votantes = await responseVotantes.Content.ReadFromJsonAsync<List<object>>();
                return View(votantes);
            }
            // Si algo falla, mandamos una lista vacía para evitar errores en la vista
            return View(new List<object>());
        }
    }
}