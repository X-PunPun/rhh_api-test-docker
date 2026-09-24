using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Domain.Empleados;
using RRHH.Domain.Vacaciones;

namespace RRHH.Application.Vacaciones;

public interface IVacacionesServicio
{
    Task<SaldoVacacionesDto> ObtenerSaldoAsync(int empleadoId, CancellationToken ct);
    Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPorEmpleadoAsync(int empleadoId, EstadoSolicitud? estado, CancellationToken ct);
    Task<SolicitudVacacionesDto> ObtenerAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPendientesDeMiEquipoAsync(CancellationToken ct);
    Task<SolicitudVacacionesDto> SolicitarAsync(int empleadoId, SolicitarVacacionesComando comando, CancellationToken ct);
    Task<SolicitudVacacionesDto> AprobarAsync(int solicitudId, CancellationToken ct);
    Task<SolicitudVacacionesDto> RechazarAsync(int solicitudId, RechazarSolicitudComando comando, CancellationToken ct);
    Task<SolicitudVacacionesDto> CancelarAsync(int solicitudId, CancellationToken ct);
}

internal sealed class VacacionesServicio(
    ISolicitudVacacionesRepositorio solicitudes,
    IFeriadoRepositorio feriados,
    IEmpleadoRepositorio empleados,
    IVacacionesConsultas consultas,
    IUsuarioActual usuarioActual,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj) : IVacacionesServicio
{
    /// <summary>Límite razonable de un período de vacaciones (evita solicitudes absurdas).</summary>
    private const int DiasCorridosMaximos = 120;

    public async Task<SaldoVacacionesDto> ObtenerSaldoAsync(int empleadoId, CancellationToken ct)
    {
        var empleado = await ObtenerEmpleadoAsync(empleadoId, ct);
        var hoy = reloj.Hoy();
        var saldo = await CalcularSaldoAsync(empleado, hoy, ct);
        var anios = empleado.AniosDeServicio(hoy);

        return new SaldoVacacionesDto(
            empleado.Id,
            hoy,
            anios,
            CalculadoraVacaciones.DiasProgresivos(empleado.AniosServicioPrevios, anios),
            saldo.Devengados,
            saldo.Tomados,
            saldo.Pendientes,
            saldo.Disponibles);
    }

    public async Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPorEmpleadoAsync(
        int empleadoId, EstadoSolicitud? estado, CancellationToken ct)
    {
        _ = await ObtenerEmpleadoAsync(empleadoId, ct);
        return await consultas.ListarPorEmpleadoAsync(empleadoId, estado, ct);
    }

    public async Task<SolicitudVacacionesDto> ObtenerAsync(int id, CancellationToken ct) =>
        await consultas.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Solicitud de vacaciones", id);

    public Task<IReadOnlyList<SolicitudVacacionesDto>> ListarPendientesDeMiEquipoAsync(CancellationToken ct) =>
        consultas.ListarPendientesDeEquipoAsync(UsuarioRequerido(), ct);

    public async Task<SolicitudVacacionesDto> SolicitarAsync(int empleadoId, SolicitarVacacionesComando comando, CancellationToken ct)
    {
        if (UsuarioRequerido() != empleadoId)
        {
            throw new AccesoDenegadoException("Solo puede solicitar vacaciones para sí mismo.");
        }

        var empleado = await ObtenerEmpleadoAsync(empleadoId, ct);
        if (!empleado.Activo)
        {
            throw new ConflictoException("El empleado está desvinculado.");
        }

        var hoy = reloj.Hoy();
        if (comando.FechaInicio < hoy)
        {
            throw new ConflictoException("No se pueden solicitar vacaciones en fechas pasadas.");
        }

        if (comando.FechaFin < comando.FechaInicio ||
            comando.FechaFin.DayNumber - comando.FechaInicio.DayNumber + 1 > DiasCorridosMaximos)
        {
            throw new ConflictoException($"El período debe ser válido y de máximo {DiasCorridosMaximos} días corridos.");
        }

        var diasFeriado = await feriados.ObtenerFechasAsync(comando.FechaInicio, comando.FechaFin, ct);
        var diasHabiles = CalculadoraVacaciones.ContarDiasHabiles(comando.FechaInicio, comando.FechaFin, diasFeriado);

        if (await solicitudes.ExisteSuperposicionAsync(empleadoId, comando.FechaInicio, comando.FechaFin, ct))
        {
            throw new ConflictoException("El período se superpone con otra solicitud pendiente o aprobada.");
        }

        var saldo = await CalcularSaldoAsync(empleado, hoy, ct);
        if (diasHabiles > saldo.Disponibles)
        {
            throw new ConflictoException(
                $"Saldo insuficiente: solicita {diasHabiles} día(s) hábil(es) y tiene {saldo.Disponibles:0.##} disponible(s).");
        }

        var solicitud = SolicitudVacaciones.Crear(
            empleadoId, comando.FechaInicio, comando.FechaFin, diasHabiles, comando.Comentario, reloj.GetUtcNow());

        solicitudes.Agregar(solicitud);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerAsync(solicitud.Id, ct);
    }

    public async Task<SolicitudVacacionesDto> AprobarAsync(int solicitudId, CancellationToken ct)
    {
        var solicitud = await ObtenerSolicitudAsync(solicitudId, ct);
        var aprobadorId = await ValidarJefaturaAsync(solicitud, ct);

        solicitud.Aprobar(aprobadorId, reloj.GetUtcNow());
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerAsync(solicitudId, ct);
    }

    public async Task<SolicitudVacacionesDto> RechazarAsync(int solicitudId, RechazarSolicitudComando comando, CancellationToken ct)
    {
        var solicitud = await ObtenerSolicitudAsync(solicitudId, ct);
        var aprobadorId = await ValidarJefaturaAsync(solicitud, ct);

        solicitud.Rechazar(aprobadorId, comando.Motivo, reloj.GetUtcNow());
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerAsync(solicitudId, ct);
    }

    public async Task<SolicitudVacacionesDto> CancelarAsync(int solicitudId, CancellationToken ct)
    {
        var solicitud = await ObtenerSolicitudAsync(solicitudId, ct);
        var usuarioId = UsuarioRequerido();

        if (usuarioId != solicitud.EmpleadoId)
        {
            throw new AccesoDenegadoException("Solo el propio empleado puede cancelar su solicitud.");
        }

        solicitud.Cancelar(usuarioId, reloj.GetUtcNow());
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerAsync(solicitudId, ct);
    }

    private async Task<SaldoVacaciones> CalcularSaldoAsync(Empleado empleado, DateOnly hoy, CancellationToken ct)
    {
        var devengados = CalculadoraVacaciones.DiasDevengados(empleado.FechaIngreso, hoy, empleado.AniosServicioPrevios);
        var tomados = await solicitudes.SumarDiasAsync(empleado.Id, EstadoSolicitud.Aprobada, ct);
        var pendientes = await solicitudes.SumarDiasAsync(empleado.Id, EstadoSolicitud.Pendiente, ct);

        return new SaldoVacaciones(devengados, tomados, pendientes);
    }

    /// <summary>Solo el jefe directo del solicitante puede resolver la solicitud.</summary>
    private async Task<int> ValidarJefaturaAsync(SolicitudVacaciones solicitud, CancellationToken ct)
    {
        var usuarioId = UsuarioRequerido();
        var jefeId = await empleados.ObtenerJefeIdAsync(solicitud.EmpleadoId, ct);

        if (jefeId != usuarioId)
        {
            throw new AccesoDenegadoException("Solo la jefatura directa puede resolver esta solicitud.");
        }

        return usuarioId;
    }

    private int UsuarioRequerido() =>
        usuarioActual.EmpleadoId ?? throw new AccesoDenegadoException("Debe identificarse para realizar esta operación.");

    private async Task<Empleado> ObtenerEmpleadoAsync(int id, CancellationToken ct) =>
        await empleados.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Empleado", id);

    private async Task<SolicitudVacaciones> ObtenerSolicitudAsync(int id, CancellationToken ct) =>
        await solicitudes.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Solicitud de vacaciones", id);
}
