using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaVotoModelos.DTOs;
using SistemaVotoMVC.Models;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            // Si ya está autenticado, redirigir según su rol para no mostrar login de nuevo
            if (User.Identity!.IsAuthenticated)
            {
                if (User.IsInRole("1")) return RedirectToAction("Main", "Admin");
                if (User.IsInRole("2")) return RedirectToAction("Index", "Votacion");
                if (User.IsInRole("3")) return RedirectToAction("Index", "Junta");
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpClientFactory.CreateClient("SistemaVotoAPI");

            // Enviamos las credenciales a la API
            var response = await client.PostAsJsonAsync("api/Aut/LoginGestion", new LoginRequestDto
            {
                Cedula = model.Cedula,
                Password = model.Password
            });

            // --- CORRECCIÓN CLAVE AQUÍ ---
            if (!response.IsSuccessStatusCode)
            {
                // Leemos el mensaje real que devuelve la API (Ej: "Usuario inactivo", "No encontrado")
                var mensajeError = await response.Content.ReadAsStringAsync();

                // Limpieza por si viene con comillas extra
                mensajeError = mensajeError.Trim('"');

                if (string.IsNullOrEmpty(mensajeError))
                    mensajeError = "Cédula o contraseña incorrecta.";

                ModelState.AddModelError("", mensajeError);
                return View(model);
            }
            // -----------------------------

            var usuario = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            if (usuario == null)
            {
                ModelState.AddModelError("", "Error al leer respuesta del servidor.");
                return View(model);
            }

            // CREACIÓN DE LA SESIÓN (COOKIE)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Cedula),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Role, usuario.RolId.ToString())
            };

            // Aseguramos que el Claim de JuntaId siempre exista para evitar errores de nulos
            claims.Add(new Claim("JuntaId", usuario.JuntaId?.ToString() ?? "0"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            // REDIRECCIÓN SEGÚN ROL
            if (usuario.RolId == 1) return RedirectToAction("Main", "Admin");
            if (usuario.RolId == 2) return RedirectToAction("Index", "Votacion");

            if (usuario.RolId == 3)
            {
                // Validación de seguridad para Jefe de Junta sin mesa asignada
                if (!usuario.JuntaId.HasValue || usuario.JuntaId == 0)
                {
                    await HttpContext.SignOutAsync();
                    ModelState.AddModelError("", "Usted es Jefe de Junta pero no tiene una mesa asignada. Contacte al administrador.");
                    return View(model);
                }
                return RedirectToAction("Index", "Junta");
            }

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