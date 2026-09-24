using Microsoft.EntityFrameworkCore;
using RRHH.Application.Vacaciones;
using RRHH.Domain.Vacaciones;

namespace RRHH.Infrastructure.Persistencia.Consultas;

internal sealed class VacacionesConsultas(RrhhDbContext db) : IVacacionesConsultas
{
    public Task<SolicitudVacacionesDto?> ObtenerAsync(int id, CancellationToken ct) =>
        Proyectar(db.SolicitudesVacaciones.AsNoTracking().Where(s => s.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPorEmpleadoAsync(
        int empleadoId, EstadoSolicitud? estado, CancellationToken ct)
    {
        var consulta = db.SolicitudesVacaciones.AsNoTracking().Where(s => s.EmpleadoId == empleadoId);
        if (estado is not null)
        {
            consulta = consulta.Where(s => s.Estado == estado);
        }

        return await Proyectar(consulta.OrderByDescending(s => s.FechaInicio)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPendientesDeEquipoAsync(int jefeId, CancellationToken ct) =>
        await Proyectar(db.SolicitudesVacaciones.AsNoTracking()
                .Where(s => s.Empleado.JefeId == jefeId && s.Estado == EstadoSolicitud.Pendiente)
                .OrderBy(s => s.FechaInicio))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FeriadoDto>> ListarFeriadosAsync(int anio, CancellationToken ct)
    {
        var desde = new DateOnly(anio, 1, 1);
        var hasta = new DateOnly(anio, 12, 31);

        return await db.Feriados.AsNoTracking()
            .Where(f => f.Fecha >= desde && f.Fecha <= hasta)
            .OrderBy(f => f.Fecha)
            .Select(f => new FeriadoDto(f.Id, f.Fecha, f.Nombre))
            .ToListAsync(ct);
    }

    private static IQueryable<SolicitudVacacionesDto> Proyectar(IQueryable<SolicitudVacaciones> consulta) =>
        consulta.Select(s => new SolicitudVacacionesDto(
            s.Id,
            s.EmpleadoId,
            s.Empleado.Nombres + " " + s.Empleado.ApellidoPaterno,
            s.FechaInicio,
            s.FechaFin,
            s.DiasHabiles,
            s.Estado,
            s.Comentario,
            s.FechaSolicitud,
            s.ResueltaPorId,
            s.FechaResolucion,
            s.MotivoRechazo));
}
