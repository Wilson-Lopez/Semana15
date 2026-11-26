using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace GatewayApi.Inicio;

// Respuesta de la raíz del gateway (/ y /gateway) para quien abra el enlace en el navegador.
// Lista las rutas de ocelot.json, así siempre coincide con la tabla de rutas.
// Solo muestra el lado del cliente (upstream): las direcciones internas de los servicios no se exponen.
public static class PaginaDeInicio
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static bool EsInicio(HttpContext contexto)
    {
        var ruta = contexto.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        return HttpMethods.IsGet(contexto.Request.Method) && ruta is "" or "/gateway";
    }

    public static Task EscribirAsync(HttpContext contexto, IConfiguration configuracion)
    {
        var rutas = configuracion.GetSection("Routes").GetChildren()
            .Select(ruta => new
            {
                metodos = ruta.GetSection("UpstreamHttpMethod").Get<string[]>() ?? [],
                ruta = ruta["UpstreamPathTemplate"],
                requiereToken = ruta.GetSection("AuthenticationOptions").Exists()
            });

        var inicio = new
        {
            servicio = "API Gateway de la clínica veterinaria (Ocelot)",
            descripcion = "Único punto de entrada a Veterinaria.Api (autenticación, propietarios y mascotas) y a VacunasApi (catálogo de vacunas).",
            comoUsar = "Obtén un token con POST /gateway/auth/login y envíalo en el encabezado Authorization: Bearer TU_TOKEN. "
                + "Las rutas con requiereToken = true las valida el gateway; el rol lo valida cada servicio.",
            rutas
        };

        return contexto.Response.WriteAsJsonAsync(inicio, Opciones);
    }
}
