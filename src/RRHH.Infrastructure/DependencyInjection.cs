using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Organizacion;
using RRHH.Application.Reportes;
using RRHH.Application.Seguros;
using RRHH.Application.Ubicacion;
using RRHH.Application.Vacaciones;
using RRHH.Infrastructure.Persistencia;
using RRHH.Infrastructure.Persistencia.Consultas;
using RRHH.Infrastructure.Persistencia.Repositorios;
using RRHH.Infrastructure.Persistencia.Semillas;
using RRHH.Infrastructure.Reportes;

namespace RRHH.Infrastructure;

public static class DependencyInjection
{
    public const string NombreCadenaConexion = "RRHH";

    /// <summary>Registra los adaptadores de salida (base de datos, Excel, etc.).</summary>
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

        services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

        // Repositorios (escritura)
        services.AddScoped<IUbicacionRepositorio, UbicacionRepositorio>();
        services.AddScoped<IDepartamentoRepositorio, DepartamentoRepositorio>();
        services.AddScoped<ICargoRepositorio, CargoRepositorio>();
        services.AddScoped<IEmpleadoRepositorio, EmpleadoRepositorio>();
        services.AddScoped<ISolicitudVacacionesRepositorio, SolicitudVacacionesRepositorio>();
        services.AddScoped<IFeriadoRepositorio, FeriadoRepositorio>();
        services.AddScoped<IPlanSeguroRepositorio, PlanSeguroRepositorio>();
        services.AddScoped<IAfiliacionSeguroRepositorio, AfiliacionSeguroRepositorio>();

        // Consultas (lectura)
        services.AddScoped<IOrganizacionConsultas, OrganizacionConsultas>();
        services.AddScoped<IEmpleadoConsultas, EmpleadoConsultas>();
        services.AddScoped<IVacacionesConsultas, VacacionesConsultas>();
        services.AddScoped<ISegurosConsultas, SegurosConsultas>();
        services.AddScoped<IReportesConsultas, ReportesConsultas>();

        // Otros adaptadores
        services.AddSingleton<IExportadorExcel, ExportadorExcelClosedXml>();

        services.AddHealthChecks().AddDbContextCheck<RrhhDbContext>("base-de-datos");

        return services;
    }

    /// <summary>Aplica migraciones pendientes. Pensado para desarrollo local y pruebas.</summary>
    public static async Task AplicarMigracionesAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RrhhDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Carga datos ficticios de demostración si la base está vacía.</summary>
    public static async Task SembrarDatosDemoAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RrhhDbContext>();
        var reloj = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        await DatosDemo.SembrarAsync(db, reloj);
    }
}
