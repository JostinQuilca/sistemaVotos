using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos.DTOs; // <--- USAMOS ESTE, que está en la biblioteca compartida
using SistemaVotoMVC.Models; // Aquí debe estar tu LoginViewModel
using System.Net.Http.Json;
using System.Security.Claims;

namespace SistemaVotoMVC.Controllers
{
    public class AutController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AutController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Si ya está logueado, redirigir según rol
            if (User.Identity!.IsAuthenticated)
            {
                if (User.IsInRole("1")) return RedirectToAction("Main", "Admin"); // Admin
                if (User.IsInRole("2")) return RedirectToAction("Index", "Votacion"); // Votante
                if (User.IsInRole("3")) return RedirectToAction("Index", "Junta"); // Jefe Junta
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Ahora usa LoginRequestDto de SistemaVotoModelos.DTOs
            var response = await client.PostAsJsonAsync("api/Aut/LoginGestion", new LoginRequestDto
            {
                Cedula = model.Cedula,
                Password = model.Password
            });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Cédula o contraseña incorrecta.");
                return View(model);
            }

            // Ahora usa LoginResponseDto de SistemaVotoModelos.DTOs
            var usuario = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            if (usuario == null)
            {
                ModelState.AddModelError("", "Error al leer respuesta del servidor.");
                return View(model);
            }

            // --- CREACIÓN DE LA SESIÓN (COOKIE) ---
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Cedula),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Role, usuario.RolId.ToString())
            };

            if (usuario.JuntaId.HasValue)
                claims.Add(new Claim("JuntaId", usuario.JuntaId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            // --- REDIRECCIÓN SEGÚN ROL ---
            if (usuario.RolId == 1) return RedirectToAction("Main", "Admin");
            if (usuario.RolId == 2) return RedirectToAction("Index", "Votacion");
            if (usuario.RolId == 3) return RedirectToAction("Index", "Junta");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}