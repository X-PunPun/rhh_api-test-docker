using System.Net;
using System.Net.Http.Json;
using RRHH.Api.IntegrationTests.Infraestructura;
using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Organizacion;
using RRHH.Application.Seguridad;
using RRHH.Domain.Seguridad;

namespace RRHH.Api.IntegrationTests;

/// <summary>Pruebas negativas de la barrera de seguridad (SECURITY-ACCEPTANCE.md).</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class SeguridadTests(ApiFactory factory)
{
    [Theory]
    [InlineData("/api/v1/empleados")]
    [InlineData("/api/v1/regiones")]
    [InlineData("/api/v1/reportes/resumen")]
    public async Task SinToken_401(string ruta)
    {
        var respuesta = await factory.CreateClient().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task TokenAlterado_401()
    {
        var tokens = await factory.IniciarSesionAsync(ApiFactory.EmailAdmin, ApiFactory.ClaveAdmin);
        var alterado = tokens.TokenAcceso[..^4] + "AAAA";

        var respuesta = await factory.ClienteConToken(alterado).GetAsync("/api/v1/regiones");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Login_ClaveIncorrecta_401_YBloqueoTrasCincoIntentos()
    {
        var empleado = await CrearEmpleadoConUsuarioAsync(Rol.Empleado);
        var anonimo = factory.CreateClient();

        for (var i = 0; i < Usuario.IntentosAntesDeBloqueo; i++)
        {
            var fallo = await anonimo.PostAsJsonAsync("/api/v1/auth/login", new LoginComando(empleado.Email, "Incorrecta.2026"), ClienteApi.Json);
            Assert.Equal(HttpStatusCode.Unauthorized, fallo.StatusCode);
        }

        var conClaveCorrecta = await anonimo.PostAsJsonAsync("/api/v1/auth/login",
            new LoginComando(empleado.Email, ApiFactory.ClaveUsuarios), ClienteApi.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, conClaveCorrecta.StatusCode);
    }

    [Fact]
    public async Task Renovar_RotaToken_YReusoInvalidaTodo()
    {
        var empleado = await CrearEmpleadoConUsuarioAsync(Rol.Empleado);
        var anonimo = factory.CreateClient();
        var inicial = await factory.IniciarSesionAsync(empleado.Email, ApiFactory.ClaveUsuarios);

        var renovado = await (await anonimo.PostAsJsonAsync("/api/v1/auth/renovar",
            new RenovarTokenComando(inicial.TokenRenovacion), ClienteApi.Json)).LeerAsync<TokensDto>();

        var reuso = await anonimo.PostAsJsonAsync("/api/v1/auth/renovar", new RenovarTokenComando(inicial.TokenRenovacion), ClienteApi.Json);
        var legitimo = await anonimo.PostAsJsonAsync("/api/v1/auth/renovar", new RenovarTokenComando(renovado.TokenRenovacion), ClienteApi.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, legitimo.StatusCode);
    }

    [Fact]
    public async Task Empleado_SoloVeSuFicha()
    {
        var yo = await CrearEmpleadoConUsuarioAsync(Rol.Empleado);
        var (dep, cargo) = await (await factory.ClienteAdminAsync()).CrearDepartamentoYCargoAsync();
        var otro = await (await factory.ClienteAdminAsync()).CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        var cliente = await factory.ClienteComoAsync(yo.Email);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/v1/empleados/{yo.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/empleados/{otro.Id}")).StatusCode);

        var busqueda = await (await cliente.GetAsync("/api/v1/empleados?tamanoPagina=100")).LeerAsync<Pagina<EmpleadoResumenDto>>();
        Assert.Equal(1, busqueda.Total);
    }

    [Fact]
    public async Task Jefatura_VeASuEquipoSinDatosPrevisionales()
    {
        var admin = await factory.ClienteAdminAsync();
        var (dep, cargo) = await admin.CrearDepartamentoYCargoAsync();
        var jefe = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        var subordinado = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, jefeId: jefe.Id));
        await admin.CrearUsuarioAsync(jefe.Id, Rol.Jefatura);
        var comoJefe = await factory.ClienteComoAsync(jefe.Email);

        var ficha = await (await comoJefe.GetAsync($"/api/v1/empleados/{subordinado.Id}")).LeerAsync<EmpleadoDetalleDto>();

        Assert.Equal(subordinado.Id, ficha.Id);
        Assert.Null(ficha.Afp);
        Assert.Null(ficha.SistemaSalud);
    }

    [Fact]
    public async Task RRHH_Regional_SoloGestionaSuRegion()
    {
        var admin = await factory.ClienteAdminAsync();
        var (dep, cargo) = await admin.CrearDepartamentoYCargoAsync();
        var enValparaiso = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, comunaId: 5109));
        var enSantiago = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, comunaId: 13101));
        var analista = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo, comunaId: 5101));
        await admin.CrearUsuarioAsync(analista.Id, Rol.RRHH, [5]);
        var rrhhV = await factory.ClienteComoAsync(analista.Email);

        Assert.Equal(HttpStatusCode.OK, (await rrhhV.GetAsync($"/api/v1/empleados/{enValparaiso.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await rrhhV.GetAsync($"/api/v1/empleados/{enSantiago.Id}")).StatusCode);

        var crearEnSantiago = await rrhhV.PostAsJsonAsync("/api/v1/empleados",
            ClienteApi.NuevoEmpleado(dep, cargo, comunaId: 13101), ClienteApi.Json);
        Assert.Equal(HttpStatusCode.Forbidden, crearEnSantiago.StatusCode);

        var crearEnVina = await rrhhV.PostAsJsonAsync("/api/v1/empleados",
            ClienteApi.NuevoEmpleado(dep, cargo, comunaId: 5109), ClienteApi.Json);
        Assert.Equal(HttpStatusCode.Created, crearEnVina.StatusCode);
    }

    [Fact]
    public async Task Empleado_NoPuedeCrearDepartamentos_NiAdministrarUsuarios()
    {
        var yo = await CrearEmpleadoConUsuarioAsync(Rol.Empleado);
        var cliente = await factory.ClienteComoAsync(yo.Email);

        var crear = await cliente.PostAsJsonAsync("/api/v1/departamentos",
            new GuardarDepartamentoComando(ClienteApi.Unico("Depto"), null), ClienteApi.Json);

        Assert.Equal(HttpStatusCode.Forbidden, crear.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/v1/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/v1/reportes/empleados/excel")).StatusCode);
    }

    [Fact]
    public async Task Auditoria_RegistraOperacionesYLogins()
    {
        var admin = await factory.ClienteAdminAsync();
        await admin.CrearDepartamentoYCargoAsync();

        var registros = await (await admin.GetAsync("/api/v1/auditoria?tamanoPagina=200")).LeerAsync<Pagina<RegistroAuditoriaDto>>();

        Assert.Contains(registros.Items, r => r.Accion.StartsWith("POST api/v1/departamentos") && r.CodigoResultado == 201);
        Assert.Contains(registros.Items, r => r.Accion == "login.exitoso" && r.Email == ApiFactory.EmailAdmin);
    }

    [Fact]
    public async Task CambiarClave_InvalidaSesionesAnteriores()
    {
        var yo = await CrearEmpleadoConUsuarioAsync(Rol.Empleado);
        var sesion = await factory.IniciarSesionAsync(yo.Email, ApiFactory.ClaveUsuarios);
        var cliente = factory.ClienteConToken(sesion.TokenAcceso);

        var cambio = await cliente.PostAsJsonAsync("/api/v1/auth/cambiar-clave",
            new CambiarClaveComando(ApiFactory.ClaveUsuarios, "Nueva.Clave2026!"), ClienteApi.Json);
        var renovar = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/renovar",
            new RenovarTokenComando(sesion.TokenRenovacion), ClienteApi.Json);

        Assert.Equal(HttpStatusCode.NoContent, cambio.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, renovar.StatusCode);
    }

    private async Task<EmpleadoDetalleDto> CrearEmpleadoConUsuarioAsync(Rol rol)
    {
        var admin = await factory.ClienteAdminAsync();
        var (dep, cargo) = await admin.CrearDepartamentoYCargoAsync();
        var empleado = await admin.CrearEmpleadoAsync(ClienteApi.NuevoEmpleado(dep, cargo));
        await admin.CrearUsuarioAsync(empleado.Id, rol);
        return empleado;
    }
}
