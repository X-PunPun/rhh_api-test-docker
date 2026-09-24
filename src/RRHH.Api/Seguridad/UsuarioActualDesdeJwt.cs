using System.Globalization;
using System.Security.Claims;
using RRHH.Application.Comun;
using RRHH.Domain.Seguridad;

namespace RRHH.Api.Seguridad;

/// <summary>
/// Adaptador de entrada: expone la identidad del JWT (ya validado por ASP.NET Core)
/// a los casos de uso a través del puerto <see cref="IUsuarioActual"/>.
/// </summary>
internal sealed class UsuarioActualDesdeJwt(IHttpContextAccessor httpContextAccessor) : IUsuarioActual
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    private bool Autenticado => Principal?.Identity?.IsAuthenticated == true;

    public int? UsuarioId => Entero("sub");

    public int? EmpleadoId => Entero("empleado_id");

    public string? Email => Autenticado ? Principal!.FindFirstValue("email") : null;

    public Rol? Rol => Autenticado && Enum.TryParse<Rol>(Principal!.FindFirstValue("role"), out var rol) ? rol : null;

    public IReadOnlyList<int> Regiones => Autenticado
        ? Principal!.FindAll("region")
            .Select(c => int.TryParse(c.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var r) ? r : 0)
            .Where(r => r > 0)
            .ToList()
        : [];

    public string? Ip => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private int? Entero(string tipo) =>
        Autenticado && int.TryParse(Principal!.FindFirstValue(tipo), NumberStyles.None, CultureInfo.InvariantCulture, out var valor)
            ? valor
            : null;
}
