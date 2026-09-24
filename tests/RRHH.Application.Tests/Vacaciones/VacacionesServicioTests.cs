using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Seguridad;
using RRHH.Application.Vacaciones;
using RRHH.Domain.Calendario;
using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;
using RRHH.Domain.Seguridad;
using RRHH.Domain.Vacaciones;

namespace RRHH.Application.Tests.Vacaciones;

/// <summary>
/// Reglas de autorización y negocio del flujo de vacaciones, probadas sin base de datos
/// gracias a que los casos de uso dependen de puertos (interfaces).
/// </summary>
public class VacacionesServicioTests
{
    private const int JefeId = 1;
    private const int EmpleadoId = 2;
    private const int OtroEmpleadoId = 3;

    // "Hoy" fijo: jueves 24-09-2026. El empleado ingresó el 01-01-2025 → 20 meses → 25 días devengados.
    private static readonly RelojFijo Reloj = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));

    private readonly FakeEmpleados _empleados = new();
    private readonly FakeSolicitudes _solicitudes = new();
    private readonly FakeFeriados _feriados = new();
    private readonly UsuarioFalso _usuario = new();

    public VacacionesServicioTests()
    {
        _empleados.Agregar(CrearEmpleado(JefeId, "11.111.111-1", jefeId: null));
        _empleados.Agregar(CrearEmpleado(EmpleadoId, "12.345.678-5", jefeId: JefeId));
        _empleados.Agregar(CrearEmpleado(OtroEmpleadoId, "7.654.321-6", jefeId: JefeId));
    }

    private VacacionesServicio Servicio() =>
        new(_solicitudes, _feriados, _empleados, new EmpleadoConsultasFalsas(_empleados), new ConsultasFalsas(_solicitudes),
            _usuario, new UnidadFalsa(), Reloj);

    private static SolicitarVacacionesComando Semana(DateOnly lunes) => new(lunes, lunes.AddDays(4), null);

    [Fact]
    public async Task Saldo_DeOtroEmpleadoSinSerSuJefe_NoEncontrado()
    {
        _usuario.EmpleadoId = OtroEmpleadoId;

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() =>
            Servicio().ObtenerSaldoAsync(EmpleadoId, CancellationToken.None));
    }

    [Fact]
    public async Task Saldo_DelSubordinado_VisibleParaElJefe()
    {
        _usuario.EmpleadoId = JefeId;

        var saldo = await Servicio().ObtenerSaldoAsync(EmpleadoId, CancellationToken.None);

        Assert.Equal(EmpleadoId, saldo.EmpleadoId);
    }

    [Fact]
    public async Task Solicitar_SinIdentificarse_AccesoDenegado()
    {
        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            Servicio().SolicitarAsync(EmpleadoId, Semana(new DateOnly(2026, 11, 2)), CancellationToken.None));
    }

    [Fact]
    public async Task Solicitar_ParaOtroEmpleado_AccesoDenegado()
    {
        _usuario.EmpleadoId = OtroEmpleadoId;

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            Servicio().SolicitarAsync(EmpleadoId, Semana(new DateOnly(2026, 11, 2)), CancellationToken.None));
    }

    [Fact]
    public async Task Solicitar_Valida_DescuentaFeriadosYQuedaPendiente()
    {
        _usuario.EmpleadoId = EmpleadoId;
        _feriados.Fechas.Add(new DateOnly(2026, 11, 2)); // lunes feriado ficticio

        var dto = await Servicio().SolicitarAsync(EmpleadoId, Semana(new DateOnly(2026, 11, 2)), CancellationToken.None);

        Assert.Equal(4, dto.DiasHabiles);
        Assert.Equal(EstadoSolicitud.Pendiente, dto.Estado);
    }

    [Fact]
    public async Task Solicitar_FechasPasadas_Conflicto()
    {
        _usuario.EmpleadoId = EmpleadoId;

        await Assert.ThrowsAsync<ConflictoException>(() =>
            Servicio().SolicitarAsync(EmpleadoId, Semana(new DateOnly(2026, 9, 7)), CancellationToken.None));
    }

    [Fact]
    public async Task Solicitar_SaldoInsuficiente_Conflicto()
    {
        _usuario.EmpleadoId = EmpleadoId;

        // 6 semanas = 30 días hábiles > 25 devengados
        var comando = new SolicitarVacacionesComando(new DateOnly(2026, 11, 2), new DateOnly(2026, 12, 11), null);

        await Assert.ThrowsAsync<ConflictoException>(() =>
            Servicio().SolicitarAsync(EmpleadoId, comando, CancellationToken.None));
    }

    [Fact]
    public async Task Solicitar_Superpuesta_Conflicto()
    {
        _usuario.EmpleadoId = EmpleadoId;
        await Servicio().SolicitarAsync(EmpleadoId, Semana(new DateOnly(2026, 11, 2)), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictoException>(() =>
            Servicio().SolicitarAsync(EmpleadoId, Semana(new DateOnly(2026, 11, 2)), CancellationToken.None));
    }

    [Fact]
    public async Task Aprobar_PorJefeDirecto_Aprobada()
    {
        var id = await CrearSolicitudPendienteAsync();
        _usuario.EmpleadoId = JefeId;

        var dto = await Servicio().AprobarAsync(id, CancellationToken.None);

        Assert.Equal(EstadoSolicitud.Aprobada, dto.Estado);
        Assert.Equal(JefeId, dto.ResueltaPorId);
    }

    [Theory]
    [InlineData(OtroEmpleadoId)] // compañero sin jefatura
    [InlineData(EmpleadoId)]     // el mismo solicitante
    public async Task Aprobar_SinSerJefeDirecto_AccesoDenegado(int usuario)
    {
        var id = await CrearSolicitudPendienteAsync();
        _usuario.EmpleadoId = usuario;

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => Servicio().AprobarAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Cancelar_PorJefe_AccesoDenegado()
    {
        var id = await CrearSolicitudPendienteAsync();
        _usuario.EmpleadoId = JefeId;

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => Servicio().CancelarAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Saldo_DescuentaPendientes()
    {
        await CrearSolicitudPendienteAsync();
        _usuario.EmpleadoId = EmpleadoId;

        var saldo = await Servicio().ObtenerSaldoAsync(EmpleadoId, CancellationToken.None);

        Assert.Equal(25m, saldo.DiasDevengados);
        Assert.Equal(5, saldo.DiasPendientesAprobacion);
        Assert.Equal(20m, saldo.DiasDisponibles);
    }

    private async Task<int> CrearSolicitudPendienteAsync()
    {
        _usuario.EmpleadoId = EmpleadoId;
        var dto = await Servicio().SolicitarAsync(EmpleadoId, Semana(new DateOnly(2026, 11, 2)), CancellationToken.None);
        _usuario.EmpleadoId = null;
        return dto.Id;
    }

    private static Empleado CrearEmpleado(int id, string rut, int? jefeId)
    {
        var empleado = Empleado.Crear(
            Rut.Crear(rut), "Nombre", "Apellido", null, $"empleado{id}@empresa.cl",
            new DateOnly(1990, 1, 1), new DateOnly(2025, 1, 1), 1, 1, 13101, Afp.Modelo, SistemaSalud.Fonasa);
        Reflexion.EstablecerId(empleado, id);
        empleado.AsignarJefe(jefeId);
        return empleado;
    }

    // ---------- Dobles de prueba de los puertos ----------

    private static class Reflexion
    {
        public static void EstablecerId(object entidad, int id) =>
            entidad.GetType().GetProperty("Id")!.GetSetMethod(nonPublic: true)!.Invoke(entidad, [id]);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    /// <summary>Usuario de prueba: al fijar EmpleadoId actúa como ese empleado; el jefe (1) tiene rol Jefatura.</summary>
    private sealed class UsuarioFalso : IUsuarioActual
    {
        public int? EmpleadoId { get; set; }
        public int? UsuarioId => EmpleadoId;
        public string? Email => null;
        public Rol? Rol => EmpleadoId switch { null => null, JefeId => Domain.Seguridad.Rol.Jefatura, _ => Domain.Seguridad.Rol.Empleado };
        public IReadOnlyList<int> Regiones => [];
        public string? Ip => null;
    }

    /// <summary>Alcance simplificado: propio empleado, equipo directo del jefe o total.</summary>
    private sealed class EmpleadoConsultasFalsas(FakeEmpleados empleados) : IEmpleadoConsultas
    {
        public async Task<bool> EstaEnAlcanceAsync(int empleadoId, AlcanceDatos alcance, CancellationToken ct)
        {
            var empleado = await empleados.ObtenerAsync(empleadoId, ct);
            return empleado is not null && (alcance.Total ||
                   alcance.EmpleadoPropioId == empleadoId || (alcance.JefeId is not null && empleado.JefeId == alcance.JefeId));
        }

        public Task<Pagina<EmpleadoResumenDto>> BuscarAsync(FiltroEmpleados filtro, AlcanceDatos alcance, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<EmpleadoDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<EmpleadoResumenDto>> ListarSubordinadosAsync(int jefeId, AlcanceDatos alcance, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<EmpleadoResumenDto>> ListarParaExportarAsync(FiltroEmpleados f, AlcanceDatos a, int m, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class UnidadFalsa : IUnidadDeTrabajo
    {
        public Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeEmpleados : IEmpleadoRepositorio
    {
        private readonly Dictionary<int, Empleado> _datos = [];

        public void Agregar(Empleado empleado) => _datos[empleado.Id] = empleado;
        public Task<Empleado?> ObtenerAsync(int id, CancellationToken ct) => Task.FromResult(_datos.GetValueOrDefault(id));
        public Task<bool> ExisteRutAsync(Rut rut, CancellationToken ct) => Task.FromResult(_datos.Values.Any(e => e.Rut == rut));
        public Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct) =>
            Task.FromResult(_datos.Values.Any(e => e.Email == email && e.Id != excluirId));
        public Task<int?> ObtenerJefeIdAsync(int empleadoId, CancellationToken ct) =>
            Task.FromResult(_datos.GetValueOrDefault(empleadoId)?.JefeId);
    }

    private sealed class FakeSolicitudes : ISolicitudVacacionesRepositorio
    {
        public List<SolicitudVacaciones> Datos { get; } = [];

        public Task<SolicitudVacaciones?> ObtenerAsync(int id, CancellationToken ct) =>
            Task.FromResult(Datos.FirstOrDefault(s => s.Id == id));

        public Task<bool> ExisteSuperposicionAsync(int empleadoId, DateOnly inicio, DateOnly fin, CancellationToken ct) =>
            Task.FromResult(Datos.Any(s => s.EmpleadoId == empleadoId &&
                s.Estado is EstadoSolicitud.Pendiente or EstadoSolicitud.Aprobada && s.SeSuperponeCon(inicio, fin)));

        public Task<int> SumarDiasAsync(int empleadoId, EstadoSolicitud estado, CancellationToken ct) =>
            Task.FromResult(Datos.Where(s => s.EmpleadoId == empleadoId && s.Estado == estado).Sum(s => s.DiasHabiles));

        public void Agregar(SolicitudVacaciones solicitud)
        {
            Reflexion.EstablecerId(solicitud, Datos.Count + 1);
            Datos.Add(solicitud);
        }
    }

    private sealed class FakeFeriados : IFeriadoRepositorio
    {
        public HashSet<DateOnly> Fechas { get; } = [];

        public Task<IReadOnlySet<DateOnly>> ObtenerFechasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
            Task.FromResult<IReadOnlySet<DateOnly>>(Fechas.Where(f => f >= desde && f <= hasta).ToHashSet());
        public Task<Feriado?> ObtenerAsync(int id, CancellationToken ct) => Task.FromResult<Feriado?>(null);
        public Task<bool> ExisteFechaAsync(DateOnly fecha, CancellationToken ct) => Task.FromResult(Fechas.Contains(fecha));
        public void Agregar(Feriado feriado) => Fechas.Add(feriado.Fecha);
        public void Eliminar(Feriado feriado) => Fechas.Remove(feriado.Fecha);
    }

    private sealed class ConsultasFalsas(FakeSolicitudes solicitudes) : IVacacionesConsultas
    {
        public Task<SolicitudVacacionesDto?> ObtenerAsync(int id, CancellationToken ct) =>
            Task.FromResult(solicitudes.Datos.Where(s => s.Id == id).Select(ADto).FirstOrDefault());

        public Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPorEmpleadoAsync(int empleadoId, EstadoSolicitud? estado, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SolicitudVacacionesDto>>(solicitudes.Datos.Where(s => s.EmpleadoId == empleadoId).Select(ADto).ToList());

        public Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPendientesDeEquipoAsync(int jefeId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SolicitudVacacionesDto>>([]);

        public Task<IReadOnlyList<FeriadoDto>> ListarFeriadosAsync(int anio, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<FeriadoDto>>([]);

        private static SolicitudVacacionesDto ADto(SolicitudVacaciones s) => new(
            s.Id, s.EmpleadoId, "Empleado", s.FechaInicio, s.FechaFin, s.DiasHabiles, s.Estado,
            s.Comentario, s.FechaSolicitud, s.ResueltaPorId, s.FechaResolucion, s.MotivoRechazo);
    }
}
