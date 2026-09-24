using RRHH.Domain.Comun;
using RRHH.Domain.Organizacion;
using RRHH.Domain.Ubicacion;

namespace RRHH.Domain.Empleados;

/// <summary>
/// Empleado interno. Las reglas de negocio viven aquí (no en el controlador),
/// por eso el estado solo cambia a través de métodos con validaciones.
/// </summary>
public sealed class Empleado : Entidad<int>
{
    public const int EdadMinimaContratacion = 18;
    public const int LargoMaximoNombre = 100;
    public const int LargoMaximoEmail = 150;

    public Rut Rut { get; private set; } = null!;
    public string Nombres { get; private set; } = null!;
    public string ApellidoPaterno { get; private set; } = null!;
    public string? ApellidoMaterno { get; private set; }
    public string Email { get; private set; } = null!;
    public DateOnly FechaNacimiento { get; private set; }
    public DateOnly FechaIngreso { get; private set; }
    public DateOnly? FechaTermino { get; private set; }

    public int DepartamentoId { get; private set; }
    public Departamento Departamento { get; private set; } = null!;

    public int CargoId { get; private set; }
    public Cargo Cargo { get; private set; } = null!;

    /// <summary>Comuna de residencia; la región se obtiene a través de ella.</summary>
    public int ComunaId { get; private set; }
    public Comuna Comuna { get; private set; } = null!;

    /// <summary>Jefatura directa (null = sin jefe, ej.: gerente general).</summary>
    public int? JefeId { get; private set; }
    public Empleado? Jefe { get; private set; }

    public bool Activo => FechaTermino is null;

    public string NombreCompleto =>
        string.Join(' ', new[] { Nombres, ApellidoPaterno, ApellidoMaterno }.Where(p => !string.IsNullOrWhiteSpace(p)));

    private Empleado() { } // EF Core

    public static Empleado Crear(
        Rut rut,
        string nombres,
        string apellidoPaterno,
        string? apellidoMaterno,
        string email,
        DateOnly fechaNacimiento,
        DateOnly fechaIngreso,
        int departamentoId,
        int cargoId,
        int comunaId)
    {
        ArgumentNullException.ThrowIfNull(rut);

        if (fechaIngreso < fechaNacimiento.AddYears(EdadMinimaContratacion))
        {
            throw new ExcepcionDominio($"El empleado debe tener al menos {EdadMinimaContratacion} años a la fecha de ingreso.");
        }

        return new Empleado
        {
            Rut = rut,
            Nombres = Guardia.TextoRequerido(nombres, nameof(Nombres), LargoMaximoNombre),
            ApellidoPaterno = Guardia.TextoRequerido(apellidoPaterno, nameof(ApellidoPaterno), LargoMaximoNombre),
            ApellidoMaterno = Guardia.TextoOpcional(apellidoMaterno, nameof(ApellidoMaterno), LargoMaximoNombre),
            Email = ValidarEmail(email),
            FechaNacimiento = fechaNacimiento,
            FechaIngreso = fechaIngreso,
            DepartamentoId = Guardia.IdPositivo(departamentoId, nameof(DepartamentoId)),
            CargoId = Guardia.IdPositivo(cargoId, nameof(CargoId)),
            ComunaId = Guardia.IdPositivo(comunaId, nameof(ComunaId)),
        };
    }

    /// <summary>
    /// Asigna la jefatura directa. La detección de ciclos (A jefe de B, B jefe de A)
    /// requiere consultar la jerarquía y se valida en la capa de aplicación.
    /// </summary>
    public void AsignarJefe(int? jefeId)
    {
        if (jefeId is not null)
        {
            Guardia.IdPositivo(jefeId.Value, nameof(JefeId));

            if (Id != 0 && jefeId.Value == Id)
            {
                throw new ExcepcionDominio("Un empleado no puede ser su propio jefe.");
            }
        }

        JefeId = jefeId;
    }

    public void CambiarAsignacion(int departamentoId, int cargoId)
    {
        AsegurarActivo();
        DepartamentoId = Guardia.IdPositivo(departamentoId, nameof(DepartamentoId));
        CargoId = Guardia.IdPositivo(cargoId, nameof(CargoId));
    }

    public void ActualizarContacto(string email, int comunaId)
    {
        Email = ValidarEmail(email);
        ComunaId = Guardia.IdPositivo(comunaId, nameof(ComunaId));
    }

    public void Desvincular(DateOnly fechaTermino)
    {
        AsegurarActivo();

        if (fechaTermino < FechaIngreso)
        {
            throw new ExcepcionDominio("La fecha de término no puede ser anterior a la fecha de ingreso.");
        }

        FechaTermino = fechaTermino;
    }

    /// <summary>Años completos de servicio a una fecha dada (base para feriado progresivo).</summary>
    public int AniosDeServicio(DateOnly aLaFecha)
    {
        var hasta = FechaTermino is { } termino && termino < aLaFecha ? termino : aLaFecha;
        if (hasta < FechaIngreso)
        {
            return 0;
        }

        var anios = hasta.Year - FechaIngreso.Year;
        if (hasta < FechaIngreso.AddYears(anios))
        {
            anios--;
        }

        return anios;
    }

    private void AsegurarActivo()
    {
        if (!Activo)
        {
            throw new ExcepcionDominio("La operación no está permitida para un empleado desvinculado.");
        }
    }

    private static string ValidarEmail(string? email)
    {
        var valor = Guardia.TextoRequerido(email, nameof(Email), LargoMaximoEmail).ToLowerInvariant();
        var arroba = valor.IndexOf('@');

        if (arroba <= 0 || arroba != valor.LastIndexOf('@') || arroba == valor.Length - 1 || !valor[(arroba + 1)..].Contains('.'))
        {
            throw new ExcepcionDominio("El email no tiene un formato válido.");
        }

        return valor;
    }
}
