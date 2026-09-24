using System.Net;
using System.Net.Http.Json;
using RRHH.Api.IntegrationTests.Infraestructura;
using RRHH.Application.Vacaciones;
using RRHH.Domain.Seguridad;
using RRHH.Domain.Vacaciones;

namespace RRHH.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public sealed class VacacionesTests(ApiFactory factory)
{
    [Fact]
    public async Task FlujoCompleto_SolicitarYAprobarPorJefeDirecto()
    {
        var admin = await factory.ClienteAdminAsync();
        var (dep, cargo) = await admin.CrearDepartamentoYCargoAsync();
        var jefe = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        var empleado = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, jefeId: jefe.Id));
        var companero = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, jefeId: jefe.Id));

        await admin.CrearUsuarioAsync(jefe.Id, Rol.Jefatura);
        await admin.CrearUsuarioAsync(empleado.Id, Rol.Empleado);
        await admin.CrearUsuarioAsync(companero.Id, Rol.Empleado);

        var comoJefe = await factory.ClienteComoAsync(jefe.Email);
        var comoEmpleado = await factory.ClienteComoAsync(empleado.Email);
        var comoCompanero = await factory.ClienteComoAsync(companero.Email);

        // 1. El empleado solicita una semana (dentro de ~2 meses, de lunes a viernes)
        var lunes = ProximoLunes(DateOnly.FromDateTime(DateTime.Today).AddDays(60));
        var respuesta = await comoEmpleado.PostAsJsonAsync($"/api/v1/empleados/{empleado.Id}/vacaciones",
            new SolicitarVacacionesComando(lunes, lunes.AddDays(4), "Descanso"), ClienteApi.Json);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var solicitud = await respuesta.LeerAsync<SolicitudVacacionesDto>();
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);

        // 2. Un compañero no puede aprobar ni ver la solicitud ajena
        Assert.Equal(HttpStatusCode.Forbidden,
            (await comoCompanero.PostAsync($"/api/v1/vacaciones/{solicitud.Id}/aprobar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await comoCompanero.GetAsync($"/api/v1/vacaciones/{solicitud.Id}")).StatusCode);

        // 3. El jefe directo la ve en su bandeja y la aprueba
        var pendientes = await (await comoJefe.GetAsync("/api/v1/vacaciones/pendientes-equipo"))
            .LeerAsync<List<SolicitudVacacionesDto>>();
        Assert.Contains(pendientes, p => p.Id == solicitud.Id);

        var aprobada = await (await comoJefe.PostAsync($"/api/v1/vacaciones/{solicitud.Id}/aprobar", null))
            .LeerAsync<SolicitudVacacionesDto>();
        Assert.Equal(EstadoSolicitud.Aprobada, aprobada.Estado);

        // 4. El saldo refleja los días tomados
        var saldo = await (await comoEmpleado.GetAsync($"/api/v1/empleados/{empleado.Id}/vacaciones/saldo"))
            .LeerAsync<SaldoVacacionesDto>();
        Assert.Equal(solicitud.DiasHabiles, saldo.DiasTomados);
    }

    [Fact]
    public async Task Solicitar_ParaOtroEmpleado_403()
    {
        var admin = await factory.ClienteAdminAsync();
        var (dep, cargo) = await admin.CrearDepartamentoYCargoAsync();
        var a = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        var b = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        await admin.CrearUsuarioAsync(b.Id, Rol.Empleado);
        var comoB = await factory.ClienteComoAsync(b.Email);

        var lunes = ProximoLunes(DateOnly.FromDateTime(DateTime.Today).AddDays(30));
        var respuesta = await comoB.PostAsJsonAsync($"/api/v1/empleados/{a.Id}/vacaciones",
            new SolicitarVacacionesComando(lunes, lunes.AddDays(1), null), ClienteApi.Json);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
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
