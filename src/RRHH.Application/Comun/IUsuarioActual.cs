using RRHH.Domain.Seguridad;

namespace RRHH.Application.Comun;

/// <summary>
/// Puerto con la identidad de quien ejecuta la operación.
/// La implementación (adaptador) lee los claims del JWT validado por ASP.NET Core;
/// los casos de uso no saben de dónde viene.
/// </summary>
public interface IUsuarioActual
{
    int? UsuarioId { get; }
    int? EmpleadoId { get; }
    string? Email { get; }
    Rol? Rol { get; }

    /// <summary>Regiones asignadas (solo rol RRHH).</summary>
    IReadOnlyList<int> Regiones { get; }

    /// <summary>IP de origen (para auditoría).</summary>
    string? Ip { get; }
}
