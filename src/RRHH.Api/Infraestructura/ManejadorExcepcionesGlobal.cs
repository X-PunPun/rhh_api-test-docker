using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RRHH.Domain.Comun;

namespace RRHH.Api.Infraestructura;

/// <summary>
/// Convierte excepciones en respuestas ProblemDetails (RFC 9457).
/// Nunca devuelve stack traces ni mensajes internos al cliente.
/// </summary>
internal sealed class ManejadorExcepcionesGlobal(
    IProblemDetailsService problemDetailsService,
    ILogger<ManejadorExcepcionesGlobal> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problema;

        if (exception is ExcepcionDominio excepcionDominio)
        {
            // Regla de negocio violada: el mensaje está pensado para el usuario.
            problema = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Regla de negocio no cumplida",
                Detail = excepcionDominio.Message,
            };
        }
        else
        {
            logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}",
                httpContext.Request.Method, httpContext.Request.Path);

            problema = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ha ocurrido un error inesperado",
            };
        }

        httpContext.Response.StatusCode = problema.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception,
        });
    }
}
