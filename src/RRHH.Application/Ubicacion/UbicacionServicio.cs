namespace RRHH.Application.Ubicacion;

internal sealed class UbicacionServicio(IUbicacionRepositorio repositorio) : IUbicacionServicio
{
    public async Task<IReadOnlyList<RegionDto>> ListarRegionesAsync(CancellationToken ct)
    {
        var regiones = await repositorio.ListarRegionesAsync(ct);
        return regiones.Select(r => new RegionDto(r.Id, r.Nombre, r.Abreviatura)).ToList();
    }

    public async Task<IReadOnlyList<ComunaDto>?> ListarComunasAsync(int regionId, CancellationToken ct)
    {
        if (!await repositorio.ExisteRegionAsync(regionId, ct))
        {
            return null;
        }

        var comunas = await repositorio.ListarComunasPorRegionAsync(regionId, ct);
        return comunas.Select(c => new ComunaDto(c.Id, c.Nombre, c.RegionId)).ToList();
    }
}
