using RRHH.Domain.Calendario;
using RRHH.Domain.Vacaciones;

namespace RRHH.Application.Vacaciones;

public interface ISolicitudVacacionesRepositorio
{
    Task<SolicitudVacaciones?> ObtenerAsync(int id, CancellationToken ct);

    /// <summary>¿Hay otra solicitud Pendiente o Aprobada que se cruce con el período?</summary>
    Task<bool> ExisteSuperposicionAsync(int empleadoId, DateOnly inicio, DateOnly fin, CancellationToken ct);

    Task<int> SumarDiasAsync(int empleadoId, EstadoSolicitud estado, CancellationToken ct);

    void Agregar(SolicitudVacaciones solicitud);
}

public interface IFeriadoRepositorio
{
    Task<IReadOnlySet<DateOnly>> ObtenerFechasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);

    Task<Feriado?> ObtenerAsync(int id, CancellationToken ct);

    Task<bool> ExisteFechaAsync(DateOnly fecha, CancellationToken ct);

    void Agregar(Feriado feriado);

    void Eliminar(Feriado feriado);
}

public interface IVacacionesConsultas
{
    Task<SolicitudVacacionesDto?> ObtenerAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPorEmpleadoAsync(int empleadoId, EstadoSolicitud? estado, CancellationToken ct);

    /// <summary>Solicitudes pendientes de los subordinados directos de un jefe.</summary>
    Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPendientesDeEquipoAsync(int jefeId, CancellationToken ct);

    Task<IReadOnlyList<FeriadoDto>> ListarFeriadosAsync(int anio, CancellationToken ct);
}
