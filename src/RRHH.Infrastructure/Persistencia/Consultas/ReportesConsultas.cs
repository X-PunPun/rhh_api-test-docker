using Microsoft.EntityFrameworkCore;
using RRHH.Application.Reportes;
using RRHH.Domain.Vacaciones;

namespace RRHH.Infrastructure.Persistencia.Consultas;

internal sealed class ReportesConsultas(RrhhDbContext db) : IReportesConsultas
{
    public async Task<ResumenDto> ObtenerResumenAsync(DateOnly hoy, CancellationToken ct)
    {
        var activos = db.Empleados.AsNoTracking().Where(e => e.FechaTermino == null);

        var totalActivos = await activos.CountAsync(ct);
        var totalDesvinculados = await db.Empleados.CountAsync(e => e.FechaTermino != null, ct);
        var pendientes = await db.SolicitudesVacaciones.CountAsync(s => s.Estado == EstadoSolicitud.Pendiente, ct);
        var afiliaciones = await db.AfiliacionesSeguro.CountAsync(
            a => a.FechaInicio <= hoy && (a.FechaTermino == null || a.FechaTermino >= hoy), ct);

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
