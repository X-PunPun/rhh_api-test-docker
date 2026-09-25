using Microsoft.Extensions.DependencyInjection;
using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Organizacion;
using RRHH.Application.Reportes;
using RRHH.Application.Seguridad;
using RRHH.Application.Seguros;
using RRHH.Application.Ubicacion;
using RRHH.Application.Vacaciones;

namespace RRHH.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso (puertos de entrada) de la aplicación.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(new RelojChile());

        services.AddScoped<IUbicacionServicio, UbicacionServicio>();
        services.AddScoped<IOrganizacionServicio, OrganizacionServicio>();
        services.AddScoped<IEmpleadoServicio, EmpleadoServicio>();
        services.AddScoped<IVacacionesServicio, VacacionesServicio>();
        services.AddScoped<IFeriadoServicio, FeriadoServicio>();
        services.AddScoped<ISegurosServicio, SegurosServicio>();
        services.AddScoped<IReportesServicio, ReportesServicio>();
        services.AddScoped<IAutenticacionServicio, AutenticacionServicio>();
        services.AddScoped<IUsuariosServicio, UsuariosServicio>();

        return services;
    }
}
