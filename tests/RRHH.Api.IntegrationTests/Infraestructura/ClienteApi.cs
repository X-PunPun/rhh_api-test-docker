using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RRHH.Application.Empleados;
using RRHH.Application.Organizacion;
using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Api.IntegrationTests.Infraestructura;

/// <summary>Utilidades para crear datos de prueba únicos a través de la propia API.</summary>
public static class ClienteApi
{
    public const string CabeceraEmpleado = "X-Empleado-Id";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Unico(string prefijo) => $"{prefijo} {Guid.NewGuid():N}"[..Math.Min(prefijo.Length + 9, 60)];

    /// <summary>Genera un RUT válido y aleatorio (dígito verificador correcto).</summary>
    public static string RutAleatorio()
    {
        var numero = Random.Shared.Next(5_000_000, 29_000_000);
        return $"{numero}-{Rut.CalcularDigitoVerificador(numero)}";
    }

    public static async Task<T> LeerAsync<T>(this HttpResponseMessage respuesta)
    {
        var valor = await respuesta.Content.ReadFromJsonAsync<T>(Json);
        return valor ?? throw new InvalidOperationException("Respuesta vacía.");
    }

    public static async Task<(int DepartamentoId, int CargoId)> CrearDepartamentoYCargoAsync(this HttpClient cliente)
    {
        var dep = await (await cliente.PostAsJsonAsync("/api/v1/departamentos",
            new GuardarDepartamentoComando(Unico("Depto"), null), Json)).LeerAsync<DepartamentoDto>();

        var cargo = await (await cliente.PostAsJsonAsync("/api/v1/cargos",
            new CrearCargoComando(Unico("Cargo"), dep.Id), Json)).LeerAsync<CargoDto>();

        return (dep.Id, cargo.Id);
    }

    public static CrearEmpleadoComando NuevoEmpleado(
        int departamentoId, int cargoId, int? jefeId = null, DateOnly? fechaIngreso = null, int comunaId = 13101) =>
        new(
            RutAleatorio(),
            "Prueba",
            "Integración",
            null,
            $"prueba.{Guid.NewGuid():N}@empresa-test.cl",
            new DateOnly(1990, 5, 10),
            fechaIngreso ?? new DateOnly(2023, 1, 2),
            departamentoId,
            cargoId,
            comunaId,
            jefeId,
            Afp.Modelo,
            SistemaSalud.Fonasa);

    public static async Task<EmpleadoDetalleDto> CrearEmpleadoAsync(this HttpClient cliente, CrearEmpleadoComando comando)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/empleados", comando, Json);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.LeerAsync<EmpleadoDetalleDto>();
    }
}
