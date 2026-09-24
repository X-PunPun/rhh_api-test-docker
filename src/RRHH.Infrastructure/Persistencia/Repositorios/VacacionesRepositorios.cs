using Microsoft.EntityFrameworkCore;
using RRHH.Application.Vacaciones;
using RRHH.Domain.Calendario;
using RRHH.Domain.Vacaciones;

namespace RRHH.Infrastructure.Persistencia.Repositorios;

internal sealed class SolicitudVacacionesRepositorio(RrhhDbContext db) : ISolicitudVacacionesRepositorio
{
    public Task<SolicitudVacaciones?> ObtenerAsync(int id, CancellationToken ct) =>
        db.SolicitudesVacaciones.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExisteSuperposicionAsync(int empleadoId, DateOnly inicio, DateOnly fin, CancellationToken ct) =>
        db.SolicitudesVacaciones.AnyAsync(s =>
            s.EmpleadoId == empleadoId &&
            (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada) &&
            s.FechaInicio <= fin && inicio <= s.FechaFin, ct);

    public Task<int> SumarDiasAsync(int empleadoId, EstadoSolicitud estado, CancellationToken ct) =>
        db.SolicitudesVacaciones
          .Where(s => s.EmpleadoId == empleadoId && s.Estado == estado)
          .SumAsync(s => s.DiasHabiles, ct);

    public void Agregar(SolicitudVacaciones solicitud) => db.SolicitudesVacaciones.Add(solicitud);
}

internal sealed class FeriadoRepositorio(RrhhDbContext db) : IFeriadoRepositorio
{
    public async Task<IReadOnlySet<DateOnly>> ObtenerFechasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var fechas = await db.Feriados.AsNoTracking()
                                      .Where(f => f.Fecha >= desde && f.Fecha <= hasta)
                                      .Select(f => f.Fecha)
                                      .ToListAsync(ct);
        return fechas.ToHashSet();
    }

    public Task<Feriado?> ObtenerAsync(int id, CancellationToken ct) =>
        db.Feriados.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<bool> ExisteFechaAsync(DateOnly fecha, CancellationToken ct) =>
        db.Feriados.AnyAsync(f => f.Fecha == fecha, ct);

    public void Agregar(Feriado feriado) => db.Feriados.Add(feriado);

    public void Eliminar(Feriado feriado) => db.Feriados.Remove(feriado);
}
