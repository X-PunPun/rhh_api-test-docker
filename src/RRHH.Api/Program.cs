using System.Text.Json.Serialization;
using RRHH.Api.Infraestructura;
using RRHH.Api.OpenApi;
using RRHH.Application;
using RRHH.Application.Comun;
using RRHH.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Servicios ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Identidad del usuario: adaptador TEMPORAL por cabecera (se reemplaza por JWT en la fase de seguridad).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualDesdeCabecera>();

// Enums como texto ("Aprobada", "Fonasa") tanto en las respuestas como en el documento OpenAPI.
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepcionesGlobal>();

// Documento OpenAPI (estándar) generado por ASP.NET Core; Swagger UI y Scalar lo muestran.
builder.Services.AddOpenApi(opciones =>
{
    opciones.AddDocumentTransformer<InformacionDocumento>();
    opciones.AddOperationTransformer<CabeceraIdentidadDemo>();
});

var app = builder.Build();

// ---------- Pipeline HTTP ----------
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Documentación solo en desarrollo (no se expone en producción).
    app.MapOpenApi();                                        // /openapi/v1.json
    app.UseSwaggerUI(opciones =>                             // /swagger
    {
        opciones.SwaggerEndpoint("/openapi/v1.json", "RRHH API v1");
        opciones.DocumentTitle = "RRHH API - Swagger";
    });
    app.MapScalarApiReference(opciones => opciones.WithTitle("RRHH API")); // /scalar

    await PrepararBaseDeDatosAsync(app);
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static async Task PrepararBaseDeDatosAsync(WebApplication app)
{
    try
    {
        if (app.Configuration.GetValue<bool>("BaseDeDatos:MigrarAlIniciar"))
        {
            await app.Services.AplicarMigracionesAsync();
        }

        if (app.Configuration.GetValue<bool>("BaseDeDatos:CargarDatosDemo"))
        {
            await app.Services.SembrarDatosDemoAsync();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex,
            "No se pudo preparar la base de datos. Verifica: 1) 'docker compose ps' muestra rrhh-sqlserver (healthy); " +
            "2) la clave en user-secrets (ConnectionStrings:RRHH) es la misma que MSSQL_SA_PASSWORD del archivo .env.");
        throw;
    }
}

// Necesario para las pruebas de integración (WebApplicationFactory<Program>).
public partial class Program { }
