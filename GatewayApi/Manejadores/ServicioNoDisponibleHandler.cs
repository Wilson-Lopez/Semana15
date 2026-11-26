using System.Net;

namespace GatewayApi.Manejadores;

// Manejador de Ocelot que se ejecuta en cada llamada del gateway a un microservicio.
// En tu equipo, si un microservicio está detenido, Ocelot no puede conectarse y responde 502 Bad Gateway.
// En Render, en cambio, si el microservicio está suspendido o no existe, quien responde es el proxy de Render
// (503 o 404, con la cabecera x-render-routing) y no el microservicio. Este manejador lo detecta y responde
// 502 igual que en local: para el cliente, el servicio de destino no está disponible.
public class ServicioNoDisponibleHandler : DelegatingHandler
{
    private const string CabeceraProxyRender = "x-render-routing";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var respuesta = await base.SendAsync(request, cancellationToken);

        // Cuando el microservicio responde de verdad, la respuesta no trae esta cabecera
        if (!respuesta.Headers.Contains(CabeceraProxyRender))
        {
            return respuesta;
        }

        respuesta.Dispose();
        return new HttpResponseMessage(HttpStatusCode.BadGateway) { RequestMessage = request };
    }
}
