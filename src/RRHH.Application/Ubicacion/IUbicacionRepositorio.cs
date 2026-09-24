using RRHH.Domain.Ubicacion;

namespace RRHH.Application.Ubicacion;

/// <summary>
/// Puerto de salida (hexagonal): la aplicación define QUÉ necesita,
/// la infraestructura (EF Core) decide CÓMO obtenerlo.
/// </summary>
public interface IUbicacionRepositorio
{
    Task<IReadOnlyList<Region>> ListarRegionesAsync(CancellationToken ct);

    Task<bool> ExisteRegionAsync(int regionId, CancellationToken ct);

    /// <summary>Región a la que pertenece la comuna (null si la comuna no existe).</summary>
    Task<int?> ObtenerRegionDeComunaAsync(int comunaId, CancellationToken ct);

    Task<IReadOnlyList<Comuna>> ListarComunasPorRegionAsync(int regionId, CancellationToken ct);
}
