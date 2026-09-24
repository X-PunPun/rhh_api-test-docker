using System.Net;
using System.Net.Http.Json;
using RRHH.Api.IntegrationTests.Infraestructura;
using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Organizacion;

namespace RRHH.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public sealed class EmpleadosTests(ApiFactory factory)
{
    private readonly HttpClient _cliente = factory.CreateClient();

    [Fact]
    public async Task CrearDepartamentoDuplicado_409()
    {
        var comando = new GuardarDepartamentoComando(ClienteApi.Unico("Depto"), null);

        var primero = await _cliente.PostAsJsonAsync("/api/v1/departamentos", comando, ClienteApi.Json);
        var segundo = await _cliente.PostAsJsonAsync("/api/v1/departamentos", comando, ClienteApi.Json);

        Assert.Equal(HttpStatusCode.Created, primero.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task CrearEmpleado_ConRegionYDetalleCompleto()
    {
        var (dep, cargo) = await _cliente.CrearDepartamentoYCargoAsync();

        var empleado = await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, comunaId: 5109));

        Assert.Equal("Viña del Mar", empleado.Comuna.Nombre);
        Assert.Equal("Valparaíso", empleado.Region.Nombre);
        Assert.True(empleado.Activo);
    }

    [Fact]
    public async Task CrearEmpleado_RutInvalido_400()
    {
        var (dep, cargo) = await _cliente.CrearDepartamentoYCargoAsync();
        var comando = ClienteApi.NuevoEmpleado(dep, cargo) with { Rut = "12.345.678-9" };

        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/empleados", comando, ClienteApi.Json);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearEmpleado_RutDuplicado_409()
    {
        var (dep, cargo) = await _cliente.CrearDepartamentoYCargoAsync();
        var comando = ClienteApi.NuevoEmpleado(dep, cargo);
        await _cliente.CrearEmpleadoAsync(comando);

        var duplicado = comando with { Email = $"otro.{Guid.NewGuid():N}@empresa-test.cl" };
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/empleados", duplicado, ClienteApi.Json);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task AsignarJefatura_QueGeneraCiclo_409()
    {
        var (dep, cargo) = await _cliente.CrearDepartamentoYCargoAsync();
        var jefe = await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        var subordinado = await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, jefeId: jefe.Id));

        // Intentar que el subordinado sea jefe de su propio jefe
        var comando = new ActualizarEmpleadoComando(
            jefe.Email, jefe.Comuna.Id, dep, cargo, subordinado.Id, jefe.Afp, jefe.SistemaSalud, 0);
        var respuesta = await _cliente.PutAsJsonAsync($"/api/v1/empleados/{jefe.Id}", comando, ClienteApi.Json);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task Buscar_FiltraPorDepartamentoYPagina()
    {
        var (dep, cargo) = await _cliente.CrearDepartamentoYCargoAsync();
        for (var i = 0; i < 3; i++)
        {
            await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        }

        var pagina = await (await _cliente.GetAsync($"/api/v1/empleados?departamentoId={dep}&tamanoPagina=2"))
            .LeerAsync<Pagina<EmpleadoResumenDto>>();

        Assert.Equal(3, pagina.Total);
        Assert.Equal(2, pagina.Items.Count);
        Assert.Equal(2, pagina.TotalPaginas);
    }

    [Fact]
    public async Task ExportarExcel_DevuelveXlsx()
    {
        var (dep, cargo) = await _cliente.CrearDepartamentoYCargoAsync();
        await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));

        var respuesta = await _cliente.GetAsync($"/api/v1/reportes/empleados/excel?departamentoId={dep}");
        var bytes = await respuesta.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal((byte)'P', bytes[0]); // un .xlsx es un ZIP: empieza con "PK"
        Assert.Equal((byte)'K', bytes[1]);
    }
}
