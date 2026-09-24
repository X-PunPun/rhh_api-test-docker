using RRHH.Application.Comun;

namespace RRHH.Api.Infraestructura;

/// <summary>
/// ADAPTADOR TEMPORAL DE DESARROLLO: identifica al usuario con la cabecera <c>X-Empleado-Id</c>.
/// No es seguro (cualquiera puede enviar la cabecera); existe solo para probar las reglas de
/// autorización antes de la fase de seguridad, donde se reemplaza por un adaptador que lee el JWT.
/// </summary>
internal sealed class UsuarioActualDesdeCabecera(IHttpContextAccessor httpContextAccessor) : IUsuarioActual
{
    public const string NombreCabecera = "X-Empleado-Id";

    public int? EmpleadoId =>
        httpContextAccessor.HttpContext?.Request.Headers[NombreCabecera].FirstOrDefault() is { } valor &&
        int.TryParse(valor, out var id) && id > 0
            ? id
            : null;
}
