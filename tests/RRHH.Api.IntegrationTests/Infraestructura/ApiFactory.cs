using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using RRHH.Infrastructure;
using Testcontainers.MsSql;

namespace RRHH.Api.IntegrationTests.Infraestructura;

/// <summary>
/// Levanta la API completa en memoria contra un SQL Server real y desechable (Testcontainers).
/// Requiere Docker Desktop en ejecución.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
        await Services.AplicarMigracionesAsync(); // crea el esquema + regiones, comunas y feriados
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var cadena = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "RRHH_Pruebas",
        }.ConnectionString;

        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.NombreCadenaConexion}", cadena);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlServer.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionApi : ICollectionFixture<ApiFactory>
{
    public const string Nombre = "API con SQL Server";
}
