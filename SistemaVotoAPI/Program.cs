using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;

// Configuración global para compatibilidad con fechas en PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configuración de la base de datos
// La API soporta múltiples entornos:
// Producción en Render mediante variable de entorno
// Desarrollo local con PostgreSQL local
// Desarrollo local conectado a PostgreSQL en Render

builder.Services.AddDbContext<APIVotosDbContext>(options =>
{
    var envConnection = Environment.GetEnvironmentVariable("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(envConnection))
    {
        options.UseNpgsql(envConnection);
        Console.WriteLine("Base de datos configurada mediante variable de entorno");
    }
    else
    {
        var localConnection = builder.Configuration
            .GetConnectionString("APIVotosDbContext.postgresql");

        if (string.IsNullOrWhiteSpace(localConnection))
        {
            localConnection = builder.Configuration
                .GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(localConnection))
        {
            throw new InvalidOperationException(
                "No se encontró ninguna cadena de conexión válida"
            );
        }

        options.UseNpgsql(localConnection);
        Console.WriteLine("Base de datos configurada mediante appsettings.json");
    }
});

// Configuración de controladores y serialización JSON
// Se ignoran referencias circulares entre entidades

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling =
            ReferenceLoopHandling.Ignore;
    });

// Configuración de Swagger para documentación y pruebas de la API

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Registrar el servicio de Email
builder.Services.AddScoped<SistemaVotoAPI.Security.EmailService>();

var app = builder.Build();

// Middleware de documentación

app.UseSwagger();
app.UseSwaggerUI();

// Inicialización de la base de datos
// En esta fase se fuerza la recreación del esquema
// Esto garantiza que todas las tablas se creen correctamente
// Este bloque es temporal mientras la base está vacía

//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;

//    try
//    {
//        var context = services.GetRequiredService<APIVotosDbContext>();

//        // Temporal para desarrollo inicial
//        context.Database.EnsureDeleted();
//        context.Database.EnsureCreated();

//        Console.WriteLine(
//            $"Base de datos inicializada correctamente: {context.Database.GetDbConnection().Database}"
//        );
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine("Error al inicializar la base de datos: " + ex.Message);
//    }
//}

// Pipeline final de la aplicación

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
