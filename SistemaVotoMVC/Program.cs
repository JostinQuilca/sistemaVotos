using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers + Views (JSON flexible para la API)
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// 2. Autenticación por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Aut/Login";           // Si no está logueado
        options.AccessDeniedPath = "/Aut/Login";    // Si no tiene rol
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// 3. Cliente HTTP para consumir la API
builder.Services.AddHttpClient("SistemaVotoAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7062/");
});

// 4. HttpContextAccessor (claims, usuario, etc.)
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 5. Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANTE: Auth primero, luego Authorization
app.UseAuthentication();
app.UseAuthorization();

// 6. Ruta por defecto → Ventana inicial
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
