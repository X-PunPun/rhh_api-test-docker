using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using RRHH.Api.Infraestructura;
using RRHH.Api.OpenApi;
using RRHH.Api.Seguridad;
using RRHH.Application;
using RRHH.Application.Comun;
using RRHH.Domain.Seguridad;
using RRHH.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Servicios ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration); // incluye autenticación JWT

// Identidad del usuario para los casos de uso (lee los claims del JWT).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualDesdeJwt>();

// Autorización: por defecto TODO exige usuario autenticado; lo público se marca con [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Politicas.Gestor, p => p.RequireRole(Politicas.RolesGestor))
    .AddPolicy(Politicas.Admin, p => p.RequireRole(nameof(Rol.Admin)));

builder.Services.AddLimitesDeUso(builder.Configuration);

// CORS: solo los orígenes del frontend configurados (el token viaja en la cabecera, sin cookies).
var origenes = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(origenes)
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithHeaders("Authorization", "Content-Type")
    .WithExposedHeaders("Content-Disposition")));

// Enums como texto ("Aprobada", "Fonasa") tanto en las respuestas como en el documento OpenAPI.
builder.Services
    .AddControllers(o => o.Filters.Add<FiltroAuditoriaAcciones>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepcionesGlobal>();

// Documento OpenAPI (estándar) generado por ASP.NET Core; Swagger UI y Scalar lo muestran.
builder.Services.AddOpenApi(opciones => opciones.AddDocumentTransformer<InformacionDocumento>());

var app = builder.Build();

// ---------- Pipeline HTTP ----------
// Detrás de un proxy (nginx en Docker): usar la IP real del cliente para el rate limiting y la auditoría.
if (app.Configuration.GetValue<bool>("Proxy:UsarCabecerasReenviadas"))
{
    var opcionesProxy = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    };
    opcionesProxy.KnownIPNetworks.Clear(); // la red interna de Docker no es fija
    opcionesProxy.KnownProxies.Clear();
    app.UseForwardedHeaders(opcionesProxy);
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCabecerasSeguridad();

if (app.Environment.IsDevelopment())
{
    // Documentación solo en desarrollo (no se expone en producción).
    app.MapOpenApi().AllowAnonymous();                        // /openapi/v1.json
    app.UseSwaggerUI(opciones =>                              // /swagger
    {
        opciones.SwaggerEndpoint("/openapi/v1.json", "RRHH API v1");
        opciones.DocumentTitle = "RRHH API - Swagger";
    });
    app.MapScalarApiReference(opciones => opciones.WithTitle("RRHH API")).AllowAnonymous(); // /scalar
}
else
{
    app.UseHsts();
}

await PrepararBaseDeDatosAsync(app);

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Migraciones y datos demo solo si la configuración lo pide (Development y pruebas);
// el administrador inicial se crea en cualquier ambiente si aún no existe ninguno.
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
            await app.Services.SembrarDatosDemoAsync(app.Configuration["Seguridad:ClaveUsuariosDemo"]);
        }

        await app.Services.CrearAdminInicialAsync(app.Configuration);
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
