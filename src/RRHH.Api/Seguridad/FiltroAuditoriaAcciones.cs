using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using RRHH.Api.Infraestructura;
using RRHH.Application.Seguridad;

namespace RRHH.Api.Seguridad;

/// <summary>
/// Registra en la bitácora toda operación que modifica datos (POST, PUT, PATCH, DELETE):
/// quién, qué ruta, cuándo y con qué resultado. Las lecturas no se registran para no llenar la tabla.
/// </summary>
internal sealed class FiltroAuditoriaAcciones(IRegistroAuditoria auditoria) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var peticion = context.HttpContext.Request;
        var ejecutado = await next();

        if (HttpMethods.IsGet(peticion.Method) || HttpMethods.IsHead(peticion.Method) || HttpMethods.IsOptions(peticion.Method))
        {
            return;
        }

        // El login/renovación se auditan dentro del caso de uso con más detalle.
        if (peticion.Path.StartsWithSegments("/api/v1/auth"))
        {
            return;
        }

        // El resultado aún no se escribe en la respuesta: el código se toma del resultado o de la excepción.
        var codigo = ejecutado switch
        {
            { Exception: { } ex, ExceptionHandled: false } => ManejadorExcepcionesGlobal.CodigoPara(ex),
            { Result: IStatusCodeActionResult { StatusCode: { } c } } => c,
            _ => StatusCodes.Status200OK,
        };

        await auditoria.RegistrarAsync(
            $"{peticion.Method} {context.ActionDescriptor.AttributeRouteInfo?.Template ?? peticion.Path}",
            peticion.Path,
            codigo,
            ct: context.HttpContext.RequestAborted);
    }
}
