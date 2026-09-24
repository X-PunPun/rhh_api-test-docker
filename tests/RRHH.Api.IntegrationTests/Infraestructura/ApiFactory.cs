using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using RRHH.Application.Seguridad;
using RRHH.Infrastructure;
using Testcontainers.MsSql;

namespace RRHH.Api.IntegrationTests.Infraestructura;

/// <summary>
/// Levanta la API completa en memoria contra un SQL Server real y desechable (Testcontainers).
/// Requiere Docker Desktop en ejecución.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string EmailAdmin = "admin@rrhh-pruebas.cl";
    public const string ClaveAdmin = "Clave.Maestra2026";
    public const string ClaveUsuarios = "Prueba.Segura2026";

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private string? _tokenAdmin;

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
        _ = Services; // arranca la API: aplica migraciones y crea el admin inicial
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var cadena = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "RRHH_Pruebas",
        }.ConnectionString;

        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.NombreCadenaConexion}", cadena);
        builder.UseSetting("BaseDeDatos:MigrarAlIniciar", "true");
        builder.UseSetting("Jwt:Llave", "llave-de-pruebas-solo-para-integracion-0123456789");
        builder.UseSetting("Seguridad:AdminInicial:Email", EmailAdmin);
        builder.UseSetting("Seguridad:AdminInicial:Clave", ClaveAdmin);
        builder.UseSetting("LimitesDeUso:LoginPorMinuto", "10000");
        builder.UseSetting("LimitesDeUso:PeticionesPorMinuto", "100000");
    }

    /// <summary>Cliente autenticado como el administrador inicial.</summary>
    public async Task<HttpClient> ClienteAdminAsync()
    {
        _tokenAdmin ??= (await IniciarSesionAsync(EmailAdmin, ClaveAdmin)).TokenAcceso;
        return ClienteConToken(_tokenAdmin);
    }

    /// <summary>Cliente autenticado con las credenciales indicadas.</summary>
    public async Task<HttpClient> ClienteComoAsync(string email, string clave = ClaveUsuarios) =>
        ClienteConToken((await IniciarSesionAsync(email, clave)).TokenAcceso);

    public async Task<TokensDto> IniciarSesionAsync(string email, string clave)
    {
        var respuesta = await CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginComando(email, clave), ClienteApi.Json);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.LeerAsync<TokensDto>();
    }

    public HttpClient ClienteConToken(string token)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
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
