using Microsoft.EntityFrameworkCore;
using RRHH.Application.Organizacion;

namespace RRHH.Infrastructure.Persistencia.Consultas;

internal sealed class OrganizacionConsultas(RrhhDbContext db) : IOrganizacionConsultas
{
    public async Task<IReadOnlyList<DepartamentoDto>> ListarDepartamentosAsync(bool? activo, CancellationToken ct)
    {
        var consulta = db.Departamentos.AsNoTracking();
        if (activo is not null)
        {
            consulta = consulta.Where(d => d.Activo == activo);
        }

        return await consulta
            .OrderBy(d => d.Nombre)
            .Select(d => new DepartamentoDto(
                d.Id,
                d.Nombre,
                d.Descripcion,
                d.Activo,
                db.Empleados.Count(e => e.DepartamentoId == d.Id && e.FechaTermino == null)))
            .ToListAsync(ct);
    }

    public Task<DepartamentoDto?> ObtenerDepartamentoAsync(int id, CancellationToken ct) =>
        db.Departamentos.AsNoTracking()
          .Where(d => d.Id == id)
          .Select(d => new DepartamentoDto(
              d.Id,
              d.Nombre,
              d.Descripcion,
              d.Activo,
              db.Empleados.Count(e => e.DepartamentoId == d.Id && e.FechaTermino == null)))
          .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CargoDto>> ListarCargosAsync(int? departamentoId, bool? activo, CancellationToken ct)
    {
        var consulta = db.Cargos.AsNoTracking();
        if (departamentoId is not null)
        {
            consulta = consulta.Where(c => c.DepartamentoId == departamentoId);
        }

        if (activo is not null)
        {
            consulta = consulta.Where(c => c.Activo == activo);
        }

        return await consulta
            .OrderBy(c => c.Departamento.Nombre).ThenBy(c => c.Nombre)
            .Select(c => new CargoDto(c.Id, c.Nombre, c.DepartamentoId, c.Departamento.Nombre, c.Activo))
            .ToListAsync(ct);
    }

    public Task<CargoDto?> ObtenerCargoAsync(int id, CancellationToken ct) =>
        db.Cargos.AsNoTracking()
          .Where(c => c.Id == id)
          .Select(c => new CargoDto(c.Id, c.Nombre, c.DepartamentoId, c.Departamento.Nombre, c.Activo))
          .FirstOrDefaultAsync(ct);
}
