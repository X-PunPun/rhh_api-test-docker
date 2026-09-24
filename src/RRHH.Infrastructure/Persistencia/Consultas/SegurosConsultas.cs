using Microsoft.EntityFrameworkCore;
using RRHH.Application.Seguros;
using RRHH.Domain.Seguros;

namespace RRHH.Infrastructure.Persistencia.Consultas;

internal sealed class SegurosConsultas(RrhhDbContext db) : ISegurosConsultas
{
    public async Task<IReadOnlyList<PlanSeguroDto>> ListarPlanesAsync(bool? activo, CancellationToken ct)
    {
        var consulta = db.PlanesSeguro.AsNoTracking();
        if (activo is not null)
        {
            consulta = consulta.Where(p => p.Activo == activo);
        }

        return await consulta
            .OrderBy(p => p.Tipo).ThenBy(p => p.Nombre)
            .Select(p => new PlanSeguroDto(p.Id, p.Nombre, p.Aseguradora, p.Tipo, p.PrimaMensualUf, p.Activo))
            .ToListAsync(ct);
    }

    public Task<PlanSeguroDto?> ObtenerPlanAsync(int id, CancellationToken ct) =>
        db.PlanesSeguro.AsNoTracking()
          .Where(p => p.Id == id)
          .Select(p => new PlanSeguroDto(p.Id, p.Nombre, p.Aseguradora, p.Tipo, p.PrimaMensualUf, p.Activo))
          .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AfiliacionSeguroDto>> ListarAfiliacionesAsync(int empleadoId, DateOnly hoy, CancellationToken ct) =>
        await Proyectar(db.AfiliacionesSeguro.AsNoTracking()
                .Where(a => a.EmpleadoId == empleadoId)
                .OrderByDescending(a => a.FechaInicio), hoy)
            .ToListAsync(ct);

    public Task<AfiliacionSeguroDto?> ObtenerAfiliacionAsync(int afiliacionId, DateOnly hoy, CancellationToken ct) =>
        Proyectar(db.AfiliacionesSeguro.AsNoTracking().Where(a => a.Id == afiliacionId), hoy).FirstOrDefaultAsync(ct);

    private static IQueryable<AfiliacionSeguroDto> Proyectar(IQueryable<AfiliacionSeguro> consulta, DateOnly hoy) =>
        consulta.Select(a => new AfiliacionSeguroDto(
            a.Id,
            a.EmpleadoId,
            a.PlanSeguroId,
            a.PlanSeguro.Nombre,
            a.PlanSeguro.Aseguradora,
            a.PlanSeguro.Tipo,
            a.PlanSeguro.PrimaMensualUf,
            a.FechaInicio,
            a.FechaTermino,
            a.NumeroCargas,
            a.FechaInicio <= hoy && (a.FechaTermino == null || a.FechaTermino >= hoy)));
}
