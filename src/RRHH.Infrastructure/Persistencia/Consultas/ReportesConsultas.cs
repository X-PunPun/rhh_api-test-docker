using Microsoft.EntityFrameworkCore;
using RRHH.Application.Reportes;
using RRHH.Application.Seguridad;
using RRHH.Domain.Vacaciones;

namespace RRHH.Infrastructure.Persistencia.Consultas;

internal sealed class ReportesConsultas(RrhhDbContext db) : IReportesConsultas
{
    public async Task<ResumenDto> ObtenerResumenAsync(DateOnly hoy, AlcanceDatos alcance, CancellationToken ct)
    {
        var visibles = EmpleadoConsultas.AplicarAlcance(db.Empleados.AsNoTracking(), alcance);
        var activos = visibles.Where(e => e.FechaTermino == null);

        var totalActivos = await activos.CountAsync(ct);
        var totalDesvinculados = await visibles.CountAsync(e => e.FechaTermino != null, ct);
        var pendientes = await db.SolicitudesVacaciones
            .CountAsync(s => s.Estado == EstadoSolicitud.Pendiente && visibles.Any(e => e.Id == s.EmpleadoId), ct);
        var afiliaciones = await db.AfiliacionesSeguro.CountAsync(a =>
            a.FechaInicio <= hoy && (a.FechaTermino == null || a.FechaTermino >= hoy) &&
            visibles.Any(e => e.Id == a.EmpleadoId), ct);

        var porRegion = await activos
            .GroupBy(e => new { e.Comuna.RegionId, e.Comuna.Region.Nombre })
            .Select(g => new ConteoDto(g.Key.RegionId, g.Key.Nombre, g.Count()))
            .ToListAsync(ct);

        var porDepartamento = await activos
            .GroupBy(e => new { e.DepartamentoId, e.Departamento.Nombre })
            .Select(g => new ConteoDto(g.Key.DepartamentoId, g.Key.Nombre, g.Count()))
            .ToListAsync(ct);

        return new ResumenDto(
            hoy,
            totalActivos,
            totalDesvinculados,
            pendientes,
            afiliaciones,
            porRegion.OrderByDescending(c => c.Cantidad).ThenBy(c => c.Nombre).ToList(),
            porDepartamento.OrderByDescending(c => c.Cantidad).ThenBy(c => c.Nombre).ToList());
    }
}
