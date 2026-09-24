using RRHH.Domain.Seguridad;

namespace RRHH.Api.Seguridad;

/// <summary>Nombres de las políticas de autorización usadas en los controladores.</summary>
public static class Politicas
{
    /// <summary>Admin o RRHH: crear y modificar datos de personal y catálogos.</summary>
    public const string Gestor = "Gestor";

    /// <summary>Solo Admin: usuarios y auditoría.</summary>
    public const string Admin = "Admin";

    public static readonly string[] RolesGestor = [nameof(Rol.Admin), nameof(Rol.RRHH)];
}
