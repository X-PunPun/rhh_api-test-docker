using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Organizacion;
using RRHH.Application.Reportes;
using RRHH.Application.Seguridad;
using RRHH.Application.Seguros;
using RRHH.Application.Ubicacion;
using RRHH.Application.Vacaciones;
using RRHH.Infrastructure.Persistencia;
using RRHH.Infrastructure.Persistencia.Consultas;
using RRHH.Infrastructure.Persistencia.Repositorios;
using RRHH.Infrastructure.Persistencia.Semillas;
using RRHH.Infrastructure.Reportes;
using RRHH.Infrastructure.Seguridad;

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

        services.AddSeguridad(configuration);

        return services;
    }

    /// <summary>Autenticación JWT, hash de claves, tokens y auditoría.</summary>
    private static IServiceCollection AddSeguridad(this IServiceCollection services, IConfiguration configuration)
    {
        var opcionesJwt = configuration.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>() ?? new OpcionesJwt();
        opcionesJwt.Validar(); // falla al iniciar, con un mensaje claro, si falta la llave
        services.AddSingleton(Options.Create(opcionesJwt));

        services.AddSingleton<IGeneradorTokens, GeneradorTokensJwt>();
        services.AddSingleton<IHasherClaves, HasherClavesPbkdf2>();
        services.AddScoped<IRegistroAuditoria, RegistroAuditoriaEf>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<ITokenRenovacionRepositorio, TokenRenovacionRepositorio>();
        services.AddScoped<IUsuarioConsultas, UsuarioConsultas>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                opciones.MapInboundClaims = false; // conserva los nombres cortos: sub, email, role
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opcionesJwt.Emisor,
                    ValidateAudience = true,
                    ValidAudience = opcionesJwt.Audiencia,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(opcionesJwt.LlaveBytes()),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "email",
                    RoleClaimType = GeneradorTokensJwt.ClaimRol,
                };
            });

        return services;
    }

    /// <summary>Aplica migraciones pendientes. Pensado para desarrollo local y pruebas.</summary>
    public static async Task AplicarMigracionesAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RrhhDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Carga datos ficticios de demostración si la base está vacía (y usuarios demo con la clave indicada).</summary>
    public static async Task SembrarDatosDemoAsync(this IServiceProvider serviceProvider, string? claveUsuariosDemo)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RrhhDbContext>();
        var reloj = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        await DatosDemo.SembrarAsync(db, reloj);

        if (!string.IsNullOrWhiteSpace(claveUsuariosDemo))
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IHasherClaves>();
            await SemillaSeguridad.CrearUsuariosDemoAsync(db, hasher, claveUsuariosDemo, reloj, CancellationToken.None);
        }
    }

    /// <summary>Crea el primer administrador si no existe ninguno (en cualquier ambiente).</summary>
    public static async Task CrearAdminInicialAsync(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        await SemillaSeguridad.CrearAdminInicialAsync(
            sp.GetRequiredService<RrhhDbContext>(),
            sp.GetRequiredService<IHasherClaves>(),
            configuration,
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("RRHH.Seguridad"),
            CancellationToken.None);
    }
}
