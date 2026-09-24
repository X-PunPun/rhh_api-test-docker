namespace RRHH.Application.Ubicacion;

/// <summary>Puerto de entrada: casos de uso del catálogo de regiones y comunas.</summary>
public interface IUbicacionServicio
{
    Task<IReadOnlyList<RegionDto>> ListarRegionesAsync(CancellationToken ct);

    /// <returns>null si la región no existe.</returns>
    Task<IReadOnlyList<ComunaDto>?> ListarComunasAsync(int regionId, CancellationToken ct);
}
