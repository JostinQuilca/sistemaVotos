using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using SistemaVotoAPI.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Configuração global para compatibilidade com datas no PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configuração da Base de Dados
builder.Services.AddDbContext<APIVotosDbContext>(options =>
{
    var envConnection = Environment.GetEnvironmentVariable("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(envConnection))
    {
        options.UseNpgsql(envConnection);
        Console.WriteLine("Base de dados configurada via variável de ambiente");
    }
    else
    {
        var localConnection = builder.Configuration.GetConnectionString("APIVotosDbContext.postgresql");

        if (string.IsNullOrWhiteSpace(localConnection))
        {
            localConnection = builder.Configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(localConnection))
        {
            throw new InvalidOperationException("Não foi encontrada nenhuma string de conexão válida");
        }

        options.UseNpgsql(localConnection);
        Console.WriteLine("Base de dados configurada via appsettings.json");
    }
});

// Configuração de Autenticação JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ClaveSecretaSuperLargaDeAlMenos32Caracteres"))
        };
    });

// Configuração de controladores e serialização JSON
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registar o serviço de Email
builder.Services.AddScoped<SistemaVotoAPI.Security.EmailService>();

var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// O ORDEM É IMPORTANTE: Autenticação antes de Autorização
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();