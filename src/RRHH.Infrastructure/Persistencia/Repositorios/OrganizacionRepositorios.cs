using Microsoft.EntityFrameworkCore;
using RRHH.Application.Organizacion;
using RRHH.Domain.Organizacion;

namespace RRHH.Infrastructure.Persistencia.Repositorios;

internal sealed class DepartamentoRepositorio(RrhhDbContext db) : IDepartamentoRepositorio
{
    public Task<Departamento?> ObtenerAsync(int id, CancellationToken ct) =>
        db.Departamentos.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct) =>
        db.Departamentos.AnyAsync(d => d.Nombre == nombre && d.Id != excluirId, ct);

    public void Agregar(Departamento departamento) => db.Departamentos.Add(departamento);
}

internal sealed class CargoRepositorio(RrhhDbContext db) : ICargoRepositorio
{
    public Task<Cargo?> ObtenerAsync(int id, CancellationToken ct) =>
        db.Cargos.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExisteNombreAsync(int departamentoId, string nombre, int? excluirId, CancellationToken ct) =>
        db.Cargos.AnyAsync(c => c.DepartamentoId == departamentoId && c.Nombre == nombre && c.Id != excluirId, ct);

    public void Agregar(Cargo cargo) => db.Cargos.Add(cargo);
}
