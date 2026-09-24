using System.Net;
using RRHH.Api.IntegrationTests.Infraestructura;
using RRHH.Application.Ubicacion;

namespace RRHH.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public sealed class CatalogosTests(ApiFactory factory)
{

    [Fact]
    public async Task Health_BaseDeDatosDisponible_SinAutenticacion()
    {
        var respuesta = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Regiones_Son16OrdenadasDeNorteASur()
    {
        var cliente = await factory.ClienteAdminAsync();
        var regiones = await (await cliente.GetAsync("/api/v1/regiones")).LeerAsync<List<RegionDto>>();

        Assert.Equal(16, regiones.Count);
        Assert.Equal("XV", regiones[0].Abreviatura);
        Assert.Equal("XII", regiones[^1].Abreviatura);
    }

    [Fact]
    public async Task ComunasDeRegionMetropolitana_Son52()
    {
        var cliente = await factory.ClienteAdminAsync();
        var comunas = await (await cliente.GetAsync("/api/v1/regiones/13/comunas")).LeerAsync<List<ComunaDto>>();

        Assert.Equal(52, comunas.Count);
    }

    [Fact]
    public async Task ComunasDeRegionInexistente_404()
    {
        var cliente = await factory.ClienteAdminAsync();
        var respuesta = await cliente.GetAsync("/api/v1/regiones/99/comunas");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
