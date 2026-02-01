using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos;
using SistemaVotoModelos.DTOs;
using System.Net.Http.Json;
using System.Security.Claims; // Necesario para obtener la cédula del Admin

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
        private readonly string _endpointJuntas = "api/Juntas";
        private readonly string _endpointDirecciones = "api/Direcciones";

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ==========================================
        // PANEL PRINCIPAL (MODIFICADO PARA VERIFICAR VOTO ADMIN)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Main()
        {
            ViewBag.NombreAdmin = User.Identity?.Name ?? "Administrador";

            // --- LÓGICA NUEVA: Verificar si el Admin ya votó ---
            var cedula = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool yaVoto = true; // Por defecto true para no mostrar el botón si hay error

            if (!string.IsNullOrEmpty(cedula))
            {
                var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
                var response = await client.GetAsync($"api/Aut/VerificarEstado/{cedula}");

                if (response.IsSuccessStatusCode)
                {
                    yaVoto = await response.Content.ReadFromJsonAsync<bool>();
                }
            }

            ViewBag.AdminYaVoto = yaVoto;
            // ---------------------------------------------------

            return View();
        }

        // ==========================================
        // GESTIÓN DE VOTANTES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GestionVotantes()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 1. Obtener Votantes
            var responseVotantes = await client.GetAsync(_endpointVotantes);
            var listaVotantes = responseVotantes.IsSuccessStatusCode
                ? await responseVotantes.Content.ReadFromJsonAsync<List<Votante>>() ?? new List<Votante>()
                : new List<Votante>();

            if (!responseVotantes.IsSuccessStatusCode)
                TempData["Error"] = "No se pudo obtener la lista de votantes.";

            // 2. Obtener Juntas
            var responseJuntas = await client.GetAsync(_endpointJuntas);
            var listaJuntas = responseJuntas.IsSuccessStatusCode
                ? await responseJuntas.Content.ReadFromJsonAsync<List<JuntaDetalleDto>>() ?? new List<JuntaDetalleDto>()
                : new List<JuntaDetalleDto>();

            ViewBag.JuntasDisponibles = listaJuntas.OrderBy(j => j.NumeroMesa).ToList();

            return View(listaVotantes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearVotante(Votante v)
        {
            if (v == null) return RedirectToAction(nameof(GestionVotantes));

            v.Cedula = (v.Cedula ?? "").Trim();
            v.NombreCompleto = (v.NombreCompleto ?? "").Trim();
            v.Email = (v.Email ?? "").Trim();
            v.FotoUrl = (v.FotoUrl ?? "").Trim();

            if (string.IsNullOrWhiteSpace(v.Password))
            {
                TempData["Error"] = "La contraseña es obligatoria para crear un usuario.";
                return RedirectToAction(nameof(GestionVotantes));
            }

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
            if (c.Id <= 0 || c.EleccionId <= 0)
            {
                TempData["Error"] = "Error: Datos del candidato no válidos.";
                return RedirectToAction(nameof(GestionCandidatos), new { eleccionId = c.EleccionId });
            }

            c.RolPostulante = (c.RolPostulante ?? "").Trim();

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsJsonAsync($"{_endpointCandidatos}/{c.Id}", c);

            if (response.IsSuccessStatusCode) TempData["Mensaje"] = "Candidato actualizado correctamente.";
            else TempData["Error"] = "Error al actualizar candidato.";

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
        // RESULTADOS DE LA ELECCIÓN (MODIFICADO PARA API EN VIVO)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> VerResultados(int eleccionId)
        {
            if (eleccionId <= 0) return RedirectToAction(nameof(GestionElecciones));

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // --- LÓGICA NUEVA: CONSUMIR ENDPOINT EN VIVO ---
            var response = await client.GetAsync($"api/Resultados/EnVivo/{eleccionId}");

            if (response.IsSuccessStatusCode)
            {
                // Obtenemos el JSON completo con porcentajes calculados por la API
                var datos = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                ViewBag.Resultados = datos;
            }
            else
            {
                TempData["Error"] = "No se pudieron cargar los resultados en vivo.";
                return RedirectToAction(nameof(GestionElecciones));
            }

            return View(); // Usa la vista que te di anteriormente
        }

        // ==========================================
        // GESTIÓN DE JUNTAS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GestionJuntas()
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // 1. Obtener Juntas
            var juntas = await client.GetFromJsonAsync<List<JuntaDetalleDto>>(_endpointJuntas);

            // 2. Obtener Direcciones
            var direcciones = await client.GetFromJsonAsync<List<Direccion>>(_endpointDirecciones);

            // 3. Obtener Posibles Jefes
            var posiblesJefes = await client.GetFromJsonAsync<List<Votante>>($"{_endpointJuntas}/PosiblesJefes");

            ViewBag.Direcciones = direcciones ?? new List<Direccion>();
            ViewBag.PosiblesJefes = posiblesJefes ?? new List<Votante>();

            return View(juntas ?? new List<JuntaDetalleDto>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPorDireccion(int direccionId, int cantidad)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            await client.PostAsync(
                $"{_endpointJuntas}/CrearPorDireccion?direccionId={direccionId}&cantidad={cantidad}",
                null);

            return RedirectToAction(nameof(GestionJuntas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarJefe(long juntaId, string cedulaJefe)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            await client.PutAsync(
                $"{_endpointJuntas}/AsignarJefe?juntaId={juntaId}&cedulaVotante={cedulaJefe}",
                null);

            return RedirectToAction(nameof(GestionJuntas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarJunta(int id)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            await client.DeleteAsync($"{_endpointJuntas}/{id}");
            return RedirectToAction(nameof(GestionJuntas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarJunta(long id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Identificador de junta no válido.";
                return RedirectToAction(nameof(GestionJuntas));
            }

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");
            var response = await client.PutAsync($"{_endpointJuntas}/AprobarJunta/{id}", null);

            if (response.IsSuccessStatusCode)
                TempData["Mensaje"] = "Junta aprobada exitosamente.";
            else
                TempData["Error"] = await response.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(GestionJuntas));
        }

        // ==========================================
        // VERIFICACIÓN DE JUNTAS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> VerificarJuntas(int? eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var respElec = await client.GetAsync(_endpointElecciones);
            var elecciones = respElec.IsSuccessStatusCode
                ? await respElec.Content.ReadFromJsonAsync<List<Eleccion>>() ?? new List<Eleccion>()
                : new List<Eleccion>();

            ViewBag.Elecciones = elecciones;

            if (eleccionId == null || eleccionId <= 0)
            {
                var activa = elecciones.FirstOrDefault(e => e.Estado == "ACTIVA");
                eleccionId = activa?.Id ?? elecciones.OrderByDescending(x => x.Id).FirstOrDefault()?.Id ?? 0;
            }

            ViewBag.EleccionId = eleccionId ?? 0;

            if ((eleccionId ?? 0) <= 0)
            {
                TempData["Error"] = "No hay elecciones disponibles.";
                return View(new List<JuntaDetalleDto>());
            }

            var respJuntas = await client.GetAsync($"{_endpointJuntas}/PorEleccion/{eleccionId}");
            var juntas = respJuntas.IsSuccessStatusCode
                ? await respJuntas.Content.ReadFromJsonAsync<List<JuntaDetalleDto>>() ?? new List<JuntaDetalleDto>()
                : new List<JuntaDetalleDto>();

            if (!respJuntas.IsSuccessStatusCode)
                TempData["Error"] = await respJuntas.Content.ReadAsStringAsync();

            return View(juntas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarJuntaVerificada(long id, int eleccionId)
        {
            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            var resp = await client.PutAsync($"{_endpointJuntas}/AprobarJunta/{id}", null);

            if (resp.IsSuccessStatusCode)
                TempData["Mensaje"] = "Junta aprobada.";
            else
                TempData["Error"] = await resp.Content.ReadAsStringAsync();

            return RedirectToAction(nameof(VerificarJuntas), new { eleccionId });
        }
    }
}