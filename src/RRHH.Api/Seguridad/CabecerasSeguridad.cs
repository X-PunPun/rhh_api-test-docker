namespace RRHH.Api.Seguridad;

/// <summary>Cabeceras de seguridad básicas para una API JSON.</summary>
internal static class CabecerasSeguridad
{
    public static IApplicationBuilder UseCabecerasSeguridad(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "no-referrer";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // Respuestas con datos personales: que ni el navegador ni proxies las guarden.
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                h["Cache-Control"] = "no-store";
            }

            await next();
        });
}
