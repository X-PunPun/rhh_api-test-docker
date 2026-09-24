namespace RRHH.Domain.Seguridad;

/// <summary>Roles del sistema. Determinan qué acciones puede hacer el usuario y sobre qué datos.</summary>
public enum Rol
{
    /// <summary>Acceso total, incluida la administración de usuarios.</summary>
    Admin = 1,

    /// <summary>Gestiona personal de las regiones asignadas.</summary>
    RRHH = 2,

    /// <summary>Ve y aprueba a su equipo directo.</summary>
    Jefatura = 3,

    /// <summary>Solo sus propios datos.</summary>
    Empleado = 4,
}
