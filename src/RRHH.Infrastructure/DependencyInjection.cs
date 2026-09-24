using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RRHH.Application.Ubicacion;
using RRHH.Infrastructure.Persistencia;
using RRHH.Infrastructure.Persistencia.Repositorios;

namespace RRHH.Infrastructure;

public static class DependencyInjection
{
    public const string NombreCadenaConexion = "RRHH";

    /// <summary>Registra los adaptadores de salida (base de datos, etc.).</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cadenaConexion = configuration.GetConnectionString(NombreCadenaConexion);
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión '{NombreCadenaConexion}'. " +
                "Configúrala con: dotnet user-secrets set \"ConnectionStrings:RRHH\" \"...\" --project src/RRHH.Api");
        }

        services.AddDbContext<RrhhDbContext>(opciones =>
            opciones.UseSqlServer(cadenaConexion, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", RrhhDbContext.Esquema);
                sql.EnableRetryOnFailure(maxRetryCount: 5);
            }));

        services.AddScoped<IUbicacionRepositorio, UbicacionRepositorio>();

        services.AddHealthChecks().AddDbContextCheck<RrhhDbContext>("base-de-datos");

        return services;
    }

    /// <summary>Aplica migraciones pendientes. Pensado solo para desarrollo local.</summary>
    public static async Task AplicarMigracionesAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RrhhDbContext>();
        await db.Database.MigrateAsync();
    }
}
