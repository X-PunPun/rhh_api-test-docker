using System.ComponentModel.DataAnnotations;
using RRHH.Domain.Seguridad;

namespace RRHH.Application.Seguridad;

public sealed record LoginComando(
    [Required, EmailAddress, StringLength(Usuario.LargoMaximoEmail)] string Email,
    [Required, StringLength(128)] string Clave);

public sealed record RenovarTokenComando([Required, StringLength(200)] string TokenRenovacion);

public sealed record CambiarClaveComando(
    [Required, StringLength(128)] string ClaveActual,
    [Required, StringLength(128)] string ClaveNueva);

public sealed record PerfilDto(int UsuarioId, string Email, Rol Rol, int? EmpleadoId, string? Nombre, IReadOnlyList<int> Regiones);

public sealed record TokensDto(
    string TokenAcceso,
    DateTimeOffset TokenAccesoExpiraEn,
    string TokenRenovacion,
    DateTimeOffset TokenRenovacionExpiraEn,
    PerfilDto Usuario);

public sealed record UsuarioDto(
    int Id,
    string Email,
    Rol Rol,
    int? EmpleadoId,
    string? Empleado,
    IReadOnlyList<int> Regiones,
    bool Activo,
    bool Bloqueado,
    DateTimeOffset? UltimoAcceso);

/// <summary>Si se indica <c>EmpleadoId</c>, el email se toma del empleado.</summary>
public sealed record CrearUsuarioComando(
    int? EmpleadoId,
    [EmailAddress, StringLength(Usuario.LargoMaximoEmail)] string? Email,
    Rol Rol,
    IReadOnlyList<int>? Regiones,
    [Required, StringLength(128)] string ClaveInicial);

public sealed record AsignarRolComando(Rol Rol, IReadOnlyList<int>? Regiones);

public sealed record RestablecerClaveComando([Required, StringLength(128)] string ClaveNueva);

public sealed record RegistroAuditoriaDto(
    long Id, DateTimeOffset Fecha, int? UsuarioId, string? Email, string Accion, string? Detalle, int? CodigoResultado, string? Ip);

public sealed record FiltroAuditoria
{
    public int? UsuarioId { get; init; }

    [StringLength(100)]
    public string? Accion { get; init; }

    public DateTimeOffset? Desde { get; init; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 200)]
    public int TamanoPagina { get; init; } = 50;
}
