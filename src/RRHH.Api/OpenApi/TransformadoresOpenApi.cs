using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using RRHH.Api.Infraestructura;

namespace RRHH.Api.OpenApi;

/// <summary>Título, versión y descripción del documento OpenAPI.</summary>
internal sealed class InformacionDocumento : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "RRHH API",
            Version = "v1",
            Description =
                "Módulo interno de Recursos Humanos (Chile): empleados por región y comuna, departamentos, cargos, " +
                "jefaturas, vacaciones (feriado legal y progresivo), seguros complementarios y reportes Excel. " +
                "Arquitectura hexagonal con ASP.NET Core 10 y EF Core.",
        };

        return Task.CompletedTask;
    }
}

/// <summary>Agrega la cabecera X-Empleado-Id a los endpoints marcados con <see cref="RequiereIdentidadDemoAttribute"/>.</summary>
internal sealed class CabeceraIdentidadDemo : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var requiere = context.Description.ActionDescriptor.EndpointMetadata.OfType<RequiereIdentidadDemoAttribute>().Any();
        if (!requiere)
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = UsuarioActualDesdeCabecera.NombreCabecera,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Id del empleado que realiza la operación (temporal hasta implementar JWT).",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
        });

        return Task.CompletedTask;
    }
}
