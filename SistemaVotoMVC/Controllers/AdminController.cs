using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using System.Net.Http.Json;

namespace SistemaVotoMVC.Controllers
{
    [Authorize(Roles = "1")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // Endpoints
        private readonly string _endpointVotantes = "api/Votantes";
        private readonly string _endpointElecciones = "api/Elecciones";
        private readonly string _endpointListas = "api/Listas";
        private readonly string _endpointCandidatos = "api/Candidatos";

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // PANEL PRINCIPAL
        [HttpGet]
        public IActionResult Main()
        {
            ViewBag.NombreAdmin = User.Identity?.Name ?? "Administrador";
            return View();
        }

        // ==========================================
        // GESTIÓN DE VOTANTES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GestionVotantes()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync(_endpointVotantes);

            var lista = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Votante>>() ?? new List<Votante>()
                : new List<Votante>();

            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo obtener la lista de votantes.";

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearVotante(Votante v)
        {
            if (v == null) return RedirectToAction(nameof(GestionVotantes));

            // Limpieza de datos básicos
            v.Cedula = (v.Cedula ?? "").Trim();
            v.NombreCompleto = (v.NombreCompleto ?? "").Trim();
            v.Email = (v.Email ?? "").Trim();
            v.FotoUrl = (v.FotoUrl ?? "").Trim();

            // Validación de contraseña obligatoria al crear
            if (string.IsNullOrWhiteSpace(v.Password))
            {
                TempData["Error"] = "La contraseña es obligatoria para crear un usuario.";
                return RedirectToAction(nameof(GestionVotantes));
            }

            // Si la Junta es 0 o negativa, lo mandamos como null
            if (v.JuntaId.HasValue && v.JuntaId.Value <= 0) v.JuntaId = null;

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointVotantes, v);

            if (response.IsSuccessStatusCode) TempData["Mensaje"] = "Usuario creado exitosamente.";
            else TempData["Error"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionVotantes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarVotante(Votante v)
        {
            if (v == null || string.IsNullOrWhiteSpace(v.Cedula)) return RedirectToAction(nameof(GestionVotantes));

            v.Cedula = v.Cedula.Trim();
            v.NombreCompleto = (v.NombreCompleto ?? "").Trim();
            v.Email = (v.Email ?? "").Trim();
            v.FotoUrl = (v.FotoUrl ?? "").Trim();

            // Si el campo Password viene vacío, la API mantendrá la contraseña vieja.
            if (string.IsNullOrWhiteSpace(v.Password)) v.Password = "";

            if (v.JuntaId.HasValue && v.JuntaId.Value <= 0) v.JuntaId = null;

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsJsonAsync($"{_endpointVotantes}/{v.Cedula}", v);

            if (response.IsSuccessStatusCode) TempData["Mensaje"] = "Datos actualizados.";
            else TempData["Error"] = "Error al actualizar.";

            return RedirectToAction(nameof(GestionVotantes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarVotante(string cedula)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            await client.DeleteAsync($"{_endpointVotantes}/{cedula}");
            return RedirectToAction(nameof(GestionVotantes));
        }

        // ==========================================
        // GESTIÓN DE ELECCIONES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GestionElecciones()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.GetAsync(_endpointElecciones);

            var lista = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Eleccion>>() ?? new List<Eleccion>()
                : new List<Eleccion>();

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEleccion(Eleccion e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.Titulo)) return RedirectToAction(nameof(GestionElecciones));
            e.Titulo = e.Titulo.Trim();
            e.Estado = "";

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointElecciones, e);

            if (response.IsSuccessStatusCode) TempData["Mensaje"] = "Elección creada.";
            else TempData["Error"] = "Error al crear elección.";

            return RedirectToAction(nameof(GestionElecciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEleccion(Eleccion e)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            await client.PutAsJsonAsync($"{_endpointElecciones}/{e.Id}", e);
            return RedirectToAction(nameof(GestionElecciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEleccion(int id)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            await client.DeleteAsync($"{_endpointElecciones}/{id}");
            return RedirectToAction(nameof(GestionElecciones));
        }

        // ==========================================
        // GESTIÓN DE LISTAS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GestionListas(int eleccionId)
        {
            if (eleccionId <= 0) return RedirectToAction(nameof(GestionElecciones));

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respEleccion = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            if (respEleccion.IsSuccessStatusCode)
                ViewBag.Eleccion = await respEleccion.Content.ReadFromJsonAsync<Eleccion>();

            var respListas = await client.GetAsync($"{_endpointListas}/PorEleccion/{eleccionId}");
            var listas = respListas.IsSuccessStatusCode
                ? await respListas.Content.ReadFromJsonAsync<List<Lista>>() ?? new List<Lista>()
                : new List<Lista>();

            return View(listas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearLista(Lista l)
        {
            if (l == null || l.EleccionId <= 0) return RedirectToAction(nameof(GestionElecciones));

            l.NombreLista = (l.NombreLista ?? "").Trim();
            l.LogoUrl = (l.LogoUrl ?? "").Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointListas, l);

            if (response.IsSuccessStatusCode) TempData["Mensaje"] = "Lista creada.";
            else TempData["Error"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionListas), new { eleccionId = l.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarLista(Lista l)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            await client.PutAsJsonAsync($"{_endpointListas}/{l.Id}", l);
            return RedirectToAction(nameof(GestionListas), new { eleccionId = l.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarLista(int id, int eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            await client.DeleteAsync($"{_endpointListas}/{id}");
            return RedirectToAction(nameof(GestionListas), new { eleccionId });
        }

        // ==========================================
        // GESTIÓN DE CANDIDATOS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GestionCandidatos(int eleccionId)
        {
            if (eleccionId <= 0) return RedirectToAction(nameof(GestionElecciones));

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respEleccion = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            if (!respEleccion.IsSuccessStatusCode) return RedirectToAction(nameof(GestionElecciones));
            ViewBag.Eleccion = await respEleccion.Content.ReadFromJsonAsync<Eleccion>();

            var respListas = await client.GetAsync($"{_endpointListas}/PorEleccion/{eleccionId}");
            ViewBag.Listas = respListas.IsSuccessStatusCode
                ? await respListas.Content.ReadFromJsonAsync<List<Lista>>() ?? new List<Lista>()
                : new List<Lista>();

            var respCand = await client.GetAsync($"{_endpointCandidatos}/PorEleccion/{eleccionId}");
            var candidatos = respCand.IsSuccessStatusCode
                ? await respCand.Content.ReadFromJsonAsync<List<Candidato>>() ?? new List<Candidato>()
                : new List<Candidato>();

            var respVotantes = await client.GetAsync(_endpointVotantes);
            var todosLosVotantes = respVotantes.IsSuccessStatusCode
                ? await respVotantes.Content.ReadFromJsonAsync<List<Votante>>() ?? new List<Votante>()
                : new List<Votante>();

            ViewBag.TodosLosVotantes = todosLosVotantes.OrderBy(v => v.NombreCompleto).ToList();

            return View(candidatos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCandidato(Candidato c)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PostAsJsonAsync(_endpointCandidatos, c);

            if (response.IsSuccessStatusCode) TempData["Mensaje"] = "Candidato registrado.";
            else TempData["Error"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionCandidatos), new { eleccionId = c.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCandidato(Candidato c)
        {
            // Validamos que el ID y la Elección vengan correctos
            if (c.Id <= 0 || c.EleccionId <= 0)
            {
                TempData["Error"] = "Error: Datos del candidato no válidos.";
                return RedirectToAction(nameof(GestionCandidatos), new { eleccionId = c.EleccionId });
            }

            // Limpiamos los datos de texto
            c.RolPostulante = (c.RolPostulante ?? "").Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Enviamos el objeto completo a la API
            var response = await client.PutAsJsonAsync($"{_endpointCandidatos}/{c.Id}", c);

            if (response.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "Candidato actualizado correctamente.";
            }
            else
            {
                TempData["Error"] = "Error al actualizar candidato.";
            }

            return RedirectToAction(nameof(GestionCandidatos), new { eleccionId = c.EleccionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCandidato(int id, int eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            await client.DeleteAsync($"{_endpointCandidatos}/{id}");
            return RedirectToAction(nameof(GestionCandidatos), new { eleccionId });
        }

        // ==========================================
        // RESULTADOS DE LA ELECCIÓN (ACTUALIZADO)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> VerResultados(int eleccionId)
        {
            if (eleccionId <= 0) return RedirectToAction(nameof(GestionElecciones));

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 1. Obtener datos de la elección
            var respEleccion = await client.GetAsync($"{_endpointElecciones}/{eleccionId}");
            if (!respEleccion.IsSuccessStatusCode) return RedirectToAction(nameof(GestionElecciones));
            var eleccion = await respEleccion.Content.ReadFromJsonAsync<Eleccion>();
            ViewBag.Eleccion = eleccion;

            // 2. Traemos candidatos
            var respCandidatos = await client.GetAsync($"{_endpointCandidatos}/PorEleccion/{eleccionId}");
            var candidatos = respCandidatos.IsSuccessStatusCode
                ? await respCandidatos.Content.ReadFromJsonAsync<List<Candidato>>()
                : new List<Candidato>();

            // 3. Traemos TODOS los votos y filtramos los de esta elección
            var respVotos = await client.GetAsync("api/VotosAnonimos");
            var todosVotos = respVotos.IsSuccessStatusCode
                ? await respVotos.Content.ReadFromJsonAsync<List<VotoAnonimo>>() ?? new List<VotoAnonimo>()
                : new List<VotoAnonimo>();

            var votosDeEstaEleccion = todosVotos.Where(v => v.EleccionId == eleccionId).ToList();

            // 4. Calculamos resultados
            // Creamos un diccionario: Clave = CédulaCandidato, Valor = CantidadDeVotos
            var conteo = votosDeEstaEleccion
                .GroupBy(v => v.CedulaCandidato)
                .ToDictionary(g => g.Key, g => g.Count());

            // Pasamos datos a la Vista
            ViewBag.Candidatos = candidatos;
            ViewBag.TotalVotos = votosDeEstaEleccion.Count;
            ViewBag.ConteoVotos = conteo;

            return View();
        }
    }
}