using RRHH.Api.Infraestructura;
using RRHH.Application;
using RRHH.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Servicios ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepcionesGlobal>();
builder.Services.AddOpenApi();

var app = builder.Build();

// ---------- Pipeline HTTP ----------
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Documentación solo en desarrollo (no se expone en producción).
    app.MapOpenApi();                 // /openapi/v1.json
    app.MapScalarApiReference();      // /scalar

    if (app.Configuration.GetValue<bool>("BaseDeDatos:MigrarAlIniciar"))
    {
        await app.Services.AplicarMigracionesAsync();
    }
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

// Necesario para las pruebas de integración (WebApplicationFactory<Program>).
public partial class Program { }