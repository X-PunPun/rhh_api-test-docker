using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RRHH.Api.Seguridad;

/// <summary>
/// Rate limiting: frena fuerza bruta en el login y el abuso general de la API.
/// Los límites se configuran en "LimitesDeUso" (las pruebas los suben).
/// </summary>
internal static class LimitesDeUso
{
    public const string PoliticaLogin = "login";

    public static IServiceCollection AddLimitesDeUso(this IServiceCollection services, IConfiguration configuracion)
    {
        var loginPorMinuto = configuracion.GetValue("LimitesDeUso:LoginPorMinuto", 10);
        var peticionesPorMinuto = configuracion.GetValue("LimitesDeUso:PeticionesPorMinuto", 300);

        services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.OnRejected = async (contexto, ct) =>
            {
                contexto.HttpContext.Response.Headers.RetryAfter = "60";
                await contexto.HttpContext.Response.WriteAsJsonAsync(new
                {
                    title = "Demasiadas solicitudes",
                    status = 429,
                    detail = "Espere un minuto antes de volver a intentar.",
                }, ct);
            };

            // Login y renovación: por IP.
            opciones.AddPolicy(PoliticaLogin, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = loginPorMinuto, Window = TimeSpan.FromMinutes(1) }));

            // Global: por usuario autenticado o, si no hay, por IP.
            opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                RateLimitPartition.GetFixedWindowLimiter(
                    http.User.FindFirst("sub")?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = peticionesPorMinuto, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
