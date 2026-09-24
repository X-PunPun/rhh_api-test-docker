using Microsoft.EntityFrameworkCore;
using RRHH.Application.Seguros;
using RRHH.Domain.Seguros;

namespace RRHH.Infrastructure.Persistencia.Repositorios;

internal sealed class PlanSeguroRepositorio(RrhhDbContext db) : IPlanSeguroRepositorio
{
    public Task<PlanSeguro?> ObtenerAsync(int id, CancellationToken ct) =>
        db.PlanesSeguro.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExisteNombreAsync(string nombre, string aseguradora, int? excluirId, CancellationToken ct) =>
        db.PlanesSeguro.AnyAsync(p => p.Nombre == nombre && p.Aseguradora == aseguradora && p.Id != excluirId, ct);

    public void Agregar(PlanSeguro plan) => db.PlanesSeguro.Add(plan);
}

internal sealed class AfiliacionSeguroRepositorio(RrhhDbContext db) : IAfiliacionSeguroRepositorio
{
    public Task<AfiliacionSeguro?> ObtenerAsync(int empleadoId, int afiliacionId, CancellationToken ct) =>
        db.AfiliacionesSeguro.FirstOrDefaultAsync(a => a.Id == afiliacionId && a.EmpleadoId == empleadoId, ct);

    public Task<bool> ExisteAfiliacionAbiertaAsync(int empleadoId, int planSeguroId, CancellationToken ct) =>
        db.AfiliacionesSeguro.AnyAsync(a =>
            a.EmpleadoId == empleadoId && a.PlanSeguroId == planSeguroId && a.FechaTermino == null, ct);

    public void Agregar(AfiliacionSeguro afiliacion) => db.AfiliacionesSeguro.Add(afiliacion);
}
