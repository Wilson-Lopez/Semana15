using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VacunasApi.Datos;
using VacunasApi.Seguridad;

var builder = WebApplication.CreateBuilder(args);

// Sección Jwt: emisor y audiencia en appsettings.json; la clave en user-secrets o en variables de entorno
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOpciones>()
    ?? throw new InvalidOperationException("Falta la sección Jwt en la configuración.");

if (jwt.Clave.Length < 32)
{
    throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 caracteres y ser la misma que usa Veterinaria.Api.");
}

// Valida el token con la misma clave, emisor y audiencia con que lo firmó Veterinaria.Api
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            NameClaimType = JwtRegisteredClaimNames.Email,
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

// Respuestas de error (401, 404, 500) en JSON y en español
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        var problema = context.ProblemDetails;
        problema.Title = problema.Status switch
        {
            StatusCodes.Status401Unauthorized => "No autorizado: falta el token o no es válido.",
            StatusCodes.Status404NotFound => "No se encontró el recurso solicitado.",
            StatusCodes.Status500InternalServerError => "Ocurrió un error interno en el servidor.",
            _ => problema.Title
        };
    });

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

// Todos los endpoints del grupo exigen un token válido (cualquier rol)
var vacunas = app.MapGroup("/api/vacunas").RequireAuthorization();

// GET: api/vacunas?especie=Gato
vacunas.MapGet("/", (string? especie) =>
{
    var lista = string.IsNullOrWhiteSpace(especie)
        ? VacunasEnMemoria.Lista
        : VacunasEnMemoria.Lista
            .Where(v => v.Especie.Equals(especie.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

    return Results.Ok(lista);
});

// GET: api/vacunas/5
vacunas.MapGet("/{id:int}", (int id) =>
{
    var vacuna = VacunasEnMemoria.Lista.FirstOrDefault(v => v.Id == id);

    return vacuna is null
        ? Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "No se encontró el recurso solicitado.",
            detail: $"No existe una vacuna con el id {id}.")
        : Results.Ok(vacuna);
});

app.Run();
