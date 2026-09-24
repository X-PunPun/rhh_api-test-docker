using Microsoft.Extensions.DependencyInjection;
using RRHH.Application.Ubicacion;

namespace RRHH.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso (puertos de entrada) de la aplicación.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUbicacionServicio, UbicacionServicio>();
        return services;
    }
}
