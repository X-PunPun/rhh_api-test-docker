using RRHH.Application.Ubicacion;
using RRHH.Domain.Ubicacion;

namespace RRHH.Application.Tests.Ubicacion;

public class UbicacionServicioTests
{
    /// <summary>Doble de prueba del puerto de salida (sin base de datos).</summary>
    private sealed class RepositorioFalso : IUbicacionRepositorio
    {
        private readonly List<Region> _regiones =
        [
            Region.Crear(15, "Arica y Parinacota", "XV", 1),
            Region.Crear(13, "Metropolitana de Santiago", "RM", 7),
        ];

        private readonly List<Comuna> _comunas =
        [
            Comuna.Crear(13101, "Santiago", 13),
            Comuna.Crear(13114, "Las Condes", 13),
            Comuna.Crear(15101, "Arica", 15),
        ];

        public Task<IReadOnlyList<Region>> ListarRegionesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Region>>(_regiones.OrderBy(r => r.Orden).ToList());

        public Task<bool> ExisteRegionAsync(int regionId, CancellationToken ct) =>
            Task.FromResult(_regiones.Any(r => r.Id == regionId));

        public Task<int?> ObtenerRegionDeComunaAsync(int comunaId, CancellationToken ct) =>
            Task.FromResult(_comunas.FirstOrDefault(c => c.Id == comunaId)?.RegionId);

        public Task<IReadOnlyList<Comuna>> ListarComunasPorRegionAsync(int regionId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Comuna>>(_comunas.Where(c => c.RegionId == regionId).ToList());
    }

    private readonly UbicacionServicio _servicio = new(new RepositorioFalso());

    [Fact]
    public async Task ListarRegiones_RetornaDtosEnOrdenGeografico()
    {
        var regiones = await _servicio.ListarRegionesAsync(CancellationToken.None);

        Assert.Equal(new[] { "XV", "RM" }, regiones.Select(r => r.Abreviatura));
    }

    [Fact]
    public async Task ListarComunas_RegionExistente_RetornaSoloSusComunas()
    {
        var comunas = await _servicio.ListarComunasAsync(13, CancellationToken.None);

        Assert.NotNull(comunas);
        Assert.Equal(2, comunas.Count);
        Assert.All(comunas, c => Assert.Equal(13, c.RegionId));
    }

    [Fact]
    public async Task ListarComunas_RegionInexistente_RetornaNull()
    {
        var comunas = await _servicio.ListarComunasAsync(99, CancellationToken.None);

        Assert.Null(comunas);
    }
}
