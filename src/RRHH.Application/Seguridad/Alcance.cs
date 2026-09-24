using RRHH.Application.Comun;
using RRHH.Domain.Seguridad;

namespace RRHH.Application.Seguridad;

/// <summary>
/// Qué empleados puede ver el usuario actual. Se aplica igual en fichas, listados,
/// exportaciones y reportes, para que no exista un camino alternativo con menos control.
/// </summary>
/// <param name="Total">Admin: sin restricción.</param>
/// <param name="Regiones">RRHH: empleados que viven en estas regiones.</param>
/// <param name="EmpleadoPropioId">Todos los roles con empleado: su propia ficha.</param>
/// <param name="JefeId">Jefatura: su equipo directo.</param>
public sealed record AlcanceDatos(bool Total, IReadOnlyList<int> Regiones, int? EmpleadoPropioId, int? JefeId)
{
    public static readonly AlcanceDatos Todo = new(true, [], null, null);
    public static readonly AlcanceDatos Ninguno = new(false, [], null, null);
}

public static class AlcanceExtensions
{
    public static AlcanceDatos Alcance(this IUsuarioActual usuario) => usuario.Rol switch
    {
        Rol.Admin => AlcanceDatos.Todo,
        Rol.RRHH => new AlcanceDatos(false, usuario.Regiones, usuario.EmpleadoId, null),
        Rol.Jefatura => new AlcanceDatos(false, [], usuario.EmpleadoId, usuario.EmpleadoId),
        Rol.Empleado => new AlcanceDatos(false, [], usuario.EmpleadoId, null),
        _ => AlcanceDatos.Ninguno,
    };

    /// <summary>Admin o RRHH: pueden crear y modificar datos de personal.</summary>
    public static bool EsGestor(this IUsuarioActual usuario) => usuario.Rol is Rol.Admin or Rol.RRHH;

    public static bool EsAdmin(this IUsuarioActual usuario) => usuario.Rol == Rol.Admin;

    /// <summary>¿Puede gestionar (crear/modificar) personal de esta región?</summary>
    public static bool PuedeGestionarRegion(this IUsuarioActual usuario, int regionId) =>
        usuario.Rol == Rol.Admin || (usuario.Rol == Rol.RRHH && usuario.Regiones.Contains(regionId));

    public static int UsuarioIdRequerido(this IUsuarioActual usuario) =>
        usuario.UsuarioId ?? throw new NoAutenticadoException("Debe iniciar sesión.");

    public static void ExigirGestor(this IUsuarioActual usuario)
    {
        if (!usuario.EsGestor())
        {
            throw new AccesoDenegadoException("Esta operación requiere rol Admin o RRHH.");
        }
    }

    public static void ExigirAdmin(this IUsuarioActual usuario)
    {
        if (!usuario.EsAdmin())
        {
            throw new AccesoDenegadoException("Esta operación requiere rol Admin.");
        }
    }
}
