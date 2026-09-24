using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace RRHH.Api.OpenApi;

/// <summary>Título, descripción y esquema de seguridad Bearer (botón "Authorize" en Swagger).</summary>
internal sealed class InformacionDocumento : IOpenApiDocumentTransformer
{
    public const string EsquemaBearer = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "RRHH API",
            Version = "v1",
            Description =
                "Módulo interno de Recursos Humanos (Chile): empleados por región y comuna, departamentos, cargos, " +
                "jefaturas, vacaciones (feriado legal y progresivo), seguros complementarios y reportes Excel.\n\n" +
                "**Autenticación:** use `POST /api/v1/auth/login`, copie `tokenAcceso` y péguelo en **Authorize**.",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[EsquemaBearer] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token de acceso obtenido en /api/v1/auth/login (válido 15 minutos).",
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(EsquemaBearer, document)] = [],
        });

        return Task.CompletedTask;
    }
}
