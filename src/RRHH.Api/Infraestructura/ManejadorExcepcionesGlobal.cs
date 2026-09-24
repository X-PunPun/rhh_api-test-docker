using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RRHH.Application.Comun;
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
        var problema = exception switch
        {
            ExcepcionDominio e => Crear(StatusCodes.Status400BadRequest, "Regla de negocio no cumplida", e.Message),
            RecursoNoEncontradoException e => Crear(StatusCodes.Status404NotFound, "Recurso no encontrado", e.Message),
            ConflictoException e => Crear(StatusCodes.Status409Conflict, "Conflicto con el estado actual", e.Message),
            AccesoDenegadoException e => Crear(StatusCodes.Status403Forbidden, "Acceso denegado", e.Message),
            _ => null,
        };

        if (problema is null)
        {
            logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}",
                httpContext.Request.Method, httpContext.Request.Path);

            problema = Crear(StatusCodes.Status500InternalServerError, "Ha ocurrido un error inesperado", null);
        }

        httpContext.Response.StatusCode = problema.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception,
        });
    }

    private static ProblemDetails Crear(int estado, string titulo, string? detalle) =>
        new() { Status = estado, Title = titulo, Detail = detalle };
}
