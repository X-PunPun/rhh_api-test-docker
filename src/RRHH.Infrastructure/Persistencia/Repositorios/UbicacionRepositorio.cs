using Microsoft.EntityFrameworkCore;
using RRHH.Application.Ubicacion;
using RRHH.Domain.Ubicacion;

namespace RRHH.Infrastructure.Persistencia.Repositorios;

/// <summary>Adaptador de salida: implementa el puerto IUbicacionRepositorio con EF Core.</summary>
internal sealed class UbicacionRepositorio(RrhhDbContext db) : IUbicacionRepositorio
{
    public async Task<IReadOnlyList<Region>> ListarRegionesAsync(CancellationToken ct) =>
        await db.Regiones.AsNoTracking().OrderBy(r => r.Orden).ToListAsync(ct);

    public Task<bool> ExisteRegionAsync(int regionId, CancellationToken ct) =>
        db.Regiones.AnyAsync(r => r.Id == regionId, ct);

    public async Task<IReadOnlyList<Comuna>> ListarComunasPorRegionAsync(int regionId, CancellationToken ct) =>
        await db.Comunas.AsNoTracking()
                        .Where(c => c.RegionId == regionId)
                        .OrderBy(c => c.Nombre)
                        .ToListAsync(ct);
}
