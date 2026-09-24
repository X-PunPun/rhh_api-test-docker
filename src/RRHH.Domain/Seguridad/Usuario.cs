using RRHH.Domain.Comun;

namespace RRHH.Domain.Seguridad;

/// <summary>
/// Cuenta de acceso al sistema. Normalmente vinculada a un empleado (su email es el del empleado);
/// un administrador técnico puede no tener empleado asociado.
/// La clave nunca se guarda: solo su hash (PBKDF2, generado por el adaptador de infraestructura).
/// </summary>
public sealed class Usuario : Entidad<int>
{
    public const int LargoMaximoEmail = 150;
    public const int IntentosAntesDeBloqueo = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private List<int> _regiones = [];

    public string Email { get; private set; } = null!;
    public string HashClave { get; private set; } = null!;
    public Rol Rol { get; private set; }
    public int? EmpleadoId { get; private set; }

    /// <summary>Regiones que puede gestionar un usuario RRHH (código CUT).</summary>
    public IReadOnlyList<int> Regiones => _regiones.AsReadOnly();

    public bool Activo { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }

    /// <summary>Cambia cuando cambian la clave, el rol o el estado: invalida los tokens de renovación emitidos antes.</summary>
    public string SelloSeguridad { get; private set; } = null!;

    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset? UltimoAcceso { get; private set; }

    private Usuario() { } // EF Core

    public static Usuario Crear(string email, string hashClave, Rol rol, int? empleadoId, IEnumerable<int>? regiones, DateTimeOffset ahora)
    {
        var usuario = new Usuario
        {
            Email = NormalizarEmail(email),
            HashClave = Guardia.TextoRequerido(hashClave, nameof(HashClave), 1000),
            EmpleadoId = empleadoId is { } id ? Guardia.IdPositivo(id, nameof(EmpleadoId)) : null,
            Activo = true,
            CreadoEn = ahora,
        };

        usuario.AsignarRol(rol, regiones);
        return usuario;
    }

    public static string NormalizarEmail(string? email) =>
        Guardia.TextoRequerido(email, nameof(Email), LargoMaximoEmail).ToLowerInvariant();

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta is { } hasta && hasta > ahora;

    public void AsignarRol(Rol rol, IEnumerable<int>? regiones)
    {
        if (!Enum.IsDefined(rol))
        {
            throw new ExcepcionDominio("El rol no es válido.");
        }

        var lista = (regiones ?? []).Distinct().OrderBy(r => r).ToList();

        if (rol == Rol.RRHH && lista.Count == 0)
        {
            throw new ExcepcionDominio("Un usuario RRHH debe tener al menos una región asignada.");
        }

        if (rol != Rol.RRHH && lista.Count > 0)
        {
            throw new ExcepcionDominio("Solo los usuarios RRHH tienen regiones asignadas.");
        }

        if (rol is Rol.Jefatura or Rol.Empleado && EmpleadoId is null)
        {
            throw new ExcepcionDominio("Los roles Jefatura y Empleado requieren un empleado asociado.");
        }

        if (lista.Any(r => r is < 1 or > 16))
        {
            throw new ExcepcionDominio("Las regiones deben ser códigos entre 1 y 16.");
        }

        Rol = rol;
        _regiones = lista;
        RenovarSello();
    }

    public void RegistrarAccesoExitoso(DateTimeOffset ahora)
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
        UltimoAcceso = ahora;
    }

    /// <summary>Suma un intento fallido; al quinto, bloquea la cuenta 15 minutos.</summary>
    public void RegistrarAccesoFallido(DateTimeOffset ahora)
    {
        IntentosFallidos++;
        if (IntentosFallidos >= IntentosAntesDeBloqueo)
        {
            BloqueadoHasta = ahora.Add(DuracionBloqueo);
            IntentosFallidos = 0;
        }
    }

    public void CambiarClave(string nuevoHash)
    {
        HashClave = Guardia.TextoRequerido(nuevoHash, nameof(HashClave), 1000);
        RenovarSello();
    }

    public void Desbloquear()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }

    public void Desactivar()
    {
        Activo = false;
        RenovarSello();
    }

    public void Activar() => Activo = true;

    private void RenovarSello() => SelloSeguridad = Guid.NewGuid().ToString("N");
}
