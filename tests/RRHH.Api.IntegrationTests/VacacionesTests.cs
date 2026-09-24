using System.Net;
using System.Net.Http.Json;
using RRHH.Api.IntegrationTests.Infraestructura;
using RRHH.Application.Vacaciones;
using RRHH.Domain.Vacaciones;

namespace RRHH.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public sealed class VacacionesTests(ApiFactory factory)
{
    private readonly HttpClient _cliente = factory.CreateClient();

    [Fact]
    public async Task FlujoCompleto_SolicitarYAprobarPorJefeDirecto()
    {
        var (dep, cargo) = await _cliente.CrearDepartamentoYCargoAsync();
        var jefe = await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        var empleado = await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, jefeId: jefe.Id));
        var companero = await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, jefeId: jefe.Id));

        // 1. El empleado solicita una semana (dentro de ~2 meses, de lunes a viernes)
        var lunes = ProximoLunes(DateOnly.FromDateTime(DateTime.Today).AddDays(60));
        var solicitud = await Enviar<SolicitudVacacionesDto>(
            HttpMethod.Post, $"/api/v1/empleados/{empleado.Id}/vacaciones", empleado.Id,
            new SolicitarVacacionesComando(lunes, lunes.AddDays(4), "Descanso"), HttpStatusCode.Created);

        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.InRange(solicitud.DiasHabiles, 1, 5);

        // 2. Un compañero (sin jefatura) no puede aprobar
        await Enviar<object>(HttpMethod.Post, $"/api/v1/vacaciones/{solicitud.Id}/aprobar", companero.Id, null, HttpStatusCode.Forbidden);

        // 3. El jefe directo la ve en su bandeja y la aprueba
        var pendientes = await Enviar<List<SolicitudVacacionesDto>>(
            HttpMethod.Get, "/api/v1/vacaciones/pendientes-equipo", jefe.Id, null, HttpStatusCode.OK);
        Assert.Contains(pendientes, p => p.Id == solicitud.Id);

        var aprobada = await Enviar<SolicitudVacacionesDto>(
            HttpMethod.Post, $"/api/v1/vacaciones/{solicitud.Id}/aprobar", jefe.Id, null, HttpStatusCode.OK);
        Assert.Equal(EstadoSolicitud.Aprobada, aprobada.Estado);

        // 4. El saldo refleja los días tomados
        var saldo = await (await _cliente.GetAsync($"/api/v1/empleados/{empleado.Id}/vacaciones/saldo"))
            .LeerAsync<SaldoVacacionesDto>();
        Assert.Equal(solicitud.DiasHabiles, saldo.DiasTomados);
    }

    [Fact]
    public async Task Solicitar_ParaOtroEmpleado_403()
    {
        var (dep, cargo) = await _cliente.CrearDepartamentoYCargoAsync();
        var a = await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        var b = await _cliente.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));

        var lunes = ProximoLunes(DateOnly.FromDateTime(DateTime.Today).AddDays(30));
        await Enviar<object>(HttpMethod.Post, $"/api/v1/empleados/{a.Id}/vacaciones", b.Id,
            new SolicitarVacacionesComando(lunes, lunes.AddDays(1), null), HttpStatusCode.Forbidden);
    }

    private async Task<T> Enviar<T>(HttpMethod metodo, string url, int usuarioId, object? cuerpo, HttpStatusCode esperado)
    {
        using var peticion = new HttpRequestMessage(metodo, url);
        peticion.Headers.Add(ClienteApi.CabeceraEmpleado, usuarioId.ToString());
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo, cuerpo.GetType(), options: ClienteApi.Json);
        }

        var respuesta = await _cliente.SendAsync(peticion);
        Assert.Equal(esperado, respuesta.StatusCode);

        return respuesta.IsSuccessStatusCode && typeof(T) != typeof(object)
            ? await respuesta.LeerAsync<T>()
            : default!;
    }

    private static DateOnly ProximoLunes(DateOnly fecha)
    {
        while (fecha.DayOfWeek != DayOfWeek.Monday)
        {
            fecha = fecha.AddDays(1);
        }

        return fecha;
    }
}
