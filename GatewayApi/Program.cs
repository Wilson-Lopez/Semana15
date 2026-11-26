using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using GatewayApi.Inicio;
using GatewayApi.Manejadores;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Tabla de rutas del gateway (upstream -> downstream).
// En tu equipo: ocelot.json (servicios en localhost). En la nube (entorno Production): ocelot.Production.json (hosts públicos).
var archivoRutas = builder.Environment.IsProduction() ? "ocelot.Production.json" : "ocelot.json";
builder.Configuration.AddJsonFile(archivoRutas, optional: false, reloadOnChange: true);

// Misma clave, emisor y audiencia que Veterinaria.Api. La clave va en user-secrets o en variables de entorno.
var clave = builder.Configuration["Jwt:Clave"] ?? string.Empty;
var emisor = builder.Configuration["Jwt:Emisor"];
var audiencia = builder.Configuration["Jwt:Audiencia"];

if (clave.Length < 32)
{
    throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 caracteres y ser la misma que usa Veterinaria.Api.");
}

// Esquema "Bearer": las rutas con AuthenticationOptions en ocelot.json validan el token antes de reenviar.
// El gateway solo comprueba que el token sea válido; el rol lo sigue exigiendo cada servicio.
builder.Services
    .AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = emisor,
            ValidateAudience = true,
            ValidAudience = audiencia,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave))
        };
    });

// Ocelot con un manejador global: si en la nube el microservicio está detenido, responde 502 como en local
builder.Services
    .AddOcelot(builder.Configuration)
    .AddDelegatingHandler<ServicioNoDisponibleHandler>(global: true);

var app = builder.Build();

// Página de inicio (GET / y /gateway): lista las rutas del gateway. Se responde antes de Ocelot.
app.MapWhen(PaginaDeInicio.EsInicio, rama =>
    rama.Run(contexto => PaginaDeInicio.EscribirAsync(contexto, app.Configuration)));

await app.UseOcelot();

await app.RunAsync();
