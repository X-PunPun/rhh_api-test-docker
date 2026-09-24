using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Domain.Seguros;

/// <summary>Afiliación de un empleado (y sus cargas) a un plan de seguro.</summary>
public sealed class AfiliacionSeguro : Entidad<int>
{
    public const int MaximoCargas = 15;

    public int EmpleadoId { get; private set; }
    public Empleado Empleado { get; private set; } = null!;

    public int PlanSeguroId { get; private set; }
    public PlanSeguro PlanSeguro { get; private set; } = null!;

    public DateOnly FechaInicio { get; private set; }
    public DateOnly? FechaTermino { get; private set; }
    public int NumeroCargas { get; private set; }

    private AfiliacionSeguro() { } // EF Core

    public static AfiliacionSeguro Crear(int empleadoId, int planSeguroId, DateOnly fechaInicio, int numeroCargas)
    {
        if (numeroCargas is < 0 or > MaximoCargas)
        {
            throw new ExcepcionDominio($"El número de cargas debe estar entre 0 y {MaximoCargas}.");
        }

        return new AfiliacionSeguro
        {
            EmpleadoId = Guardia.IdPositivo(empleadoId, nameof(EmpleadoId)),
            PlanSeguroId = Guardia.IdPositivo(planSeguroId, nameof(PlanSeguroId)),
            FechaInicio = fechaInicio,
            NumeroCargas = numeroCargas,
        };
    }

    public bool EstaVigente(DateOnly fecha) =>
        FechaInicio <= fecha && (FechaTermino is null || FechaTermino >= fecha);

    public void Terminar(DateOnly fechaTermino)
    {
        if (FechaTermino is not null)
        {
            throw new ExcepcionDominio("La afiliación ya se encuentra terminada.");
        }

        if (fechaTermino < FechaInicio)
        {
            throw new ExcepcionDominio("La fecha de término no puede ser anterior al inicio de la afiliación.");
        }

        FechaTermino = fechaTermino;
    }
}
