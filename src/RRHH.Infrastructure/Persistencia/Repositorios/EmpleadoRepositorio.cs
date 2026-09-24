using Microsoft.EntityFrameworkCore;
using RRHH.Application.Empleados;
using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Infrastructure.Persistencia.Repositorios;

internal sealed class EmpleadoRepositorio(RrhhDbContext db) : IEmpleadoRepositorio
{
    public Task<Empleado?> ObtenerAsync(int id, CancellationToken ct) =>
        db.Empleados.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<bool> ExisteRutAsync(Rut rut, CancellationToken ct) =>
        db.Empleados.AnyAsync(e => e.Rut == rut, ct);

    public Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct) =>
        db.Empleados.AnyAsync(e => e.Email == email && e.Id != excluirId, ct);

    public Task<int?> ObtenerJefeIdAsync(int empleadoId, CancellationToken ct) =>
        db.Empleados.Where(e => e.Id == empleadoId).Select(e => e.JefeId).FirstOrDefaultAsync(ct);

    public void Agregar(Empleado empleado) => db.Empleados.Add(empleado);
}
