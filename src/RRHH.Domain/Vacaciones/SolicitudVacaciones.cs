using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Domain.Vacaciones;

/// <summary>
/// Solicitud de feriado legal. Máquina de estados:
/// Pendiente → Aprobada | Rechazada | Cancelada (estados finales).
/// </summary>
public sealed class SolicitudVacaciones : Entidad<int>
{
    public const int LargoMaximoTexto = 500;

    public int EmpleadoId { get; private set; }
    public Empleado Empleado { get; private set; } = null!;

    public DateOnly FechaInicio { get; private set; }
    public DateOnly FechaFin { get; private set; }

    /// <summary>Días hábiles descontados (lunes a viernes, sin feriados).</summary>
    public int DiasHabiles { get; private set; }

    public EstadoSolicitud Estado { get; private set; }
    public string? Comentario { get; private set; }
    public DateTimeOffset FechaSolicitud { get; private set; }

    public int? ResueltaPorId { get; private set; }
    public DateTimeOffset? FechaResolucion { get; private set; }
    public string? MotivoRechazo { get; private set; }

    private SolicitudVacaciones() { } // EF Core

    public static SolicitudVacaciones Crear(
        int empleadoId,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        int diasHabiles,
        string? comentario,
        DateTimeOffset ahora)
    {
        if (fechaFin < fechaInicio)
        {
            throw new ExcepcionDominio("La fecha de término no puede ser anterior a la fecha de inicio.");
        }

        if (diasHabiles <= 0)
        {
            throw new ExcepcionDominio("El período solicitado no contiene días hábiles.");
        }

        return new SolicitudVacaciones
        {
            EmpleadoId = Guardia.IdPositivo(empleadoId, nameof(EmpleadoId)),
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            DiasHabiles = diasHabiles,
            Comentario = Guardia.TextoOpcional(comentario, nameof(Comentario), LargoMaximoTexto),
            Estado = EstadoSolicitud.Pendiente,
            FechaSolicitud = ahora,
        };
    }

    public void Aprobar(int aprobadorId, DateTimeOffset ahora)
    {
        ValidarResolucion(aprobadorId);
        Estado = EstadoSolicitud.Aprobada;
        ResueltaPorId = aprobadorId;
        FechaResolucion = ahora;
    }

    public void Rechazar(int aprobadorId, string motivo, DateTimeOffset ahora)
    {
        ValidarResolucion(aprobadorId);
        MotivoRechazo = Guardia.TextoRequerido(motivo, nameof(MotivoRechazo), LargoMaximoTexto);
        Estado = EstadoSolicitud.Rechazada;
        ResueltaPorId = aprobadorId;
        FechaResolucion = ahora;
    }

    public void Cancelar(int solicitanteId, DateTimeOffset ahora)
    {
        if (solicitanteId != EmpleadoId)
        {
            throw new ExcepcionDominio("Solo el propio empleado puede cancelar su solicitud.");
        }

        AsegurarPendiente();
        Estado = EstadoSolicitud.Cancelada;
        FechaResolucion = ahora;
    }

    public bool SeSuperponeCon(DateOnly inicio, DateOnly fin) => FechaInicio <= fin && inicio <= FechaFin;

    private void ValidarResolucion(int aprobadorId)
    {
        if (aprobadorId == EmpleadoId)
        {
            throw new ExcepcionDominio("Un empleado no puede resolver su propia solicitud.");
        }

        AsegurarPendiente();
    }

    private void AsegurarPendiente()
    {
        if (Estado != EstadoSolicitud.Pendiente)
        {
            throw new ExcepcionDominio($"La solicitud ya fue resuelta (estado: {Estado}).");
        }
    }
}
