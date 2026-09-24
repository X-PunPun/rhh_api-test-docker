using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Seguridad;
using RRHH.Domain.Seguros;

namespace RRHH.Application.Seguros;

public interface ISegurosServicio
{
    Task<IReadOnlyList<PlanSeguroDto>> ListarPlanesAsync(bool? activo, CancellationToken ct);
    Task<PlanSeguroDto> ObtenerPlanAsync(int id, CancellationToken ct);
    Task<PlanSeguroDto> CrearPlanAsync(GuardarPlanSeguroComando comando, CancellationToken ct);
    Task<PlanSeguroDto> ActualizarPlanAsync(int id, GuardarPlanSeguroComando comando, CancellationToken ct);
    Task CambiarEstadoPlanAsync(int id, bool activo, CancellationToken ct);

    Task<IReadOnlyList<AfiliacionSeguroDto>> ListarAfiliacionesAsync(int empleadoId, CancellationToken ct);
    Task<AfiliacionSeguroDto> AfiliarAsync(int empleadoId, AfiliarSeguroComando comando, CancellationToken ct);
    Task<AfiliacionSeguroDto> TerminarAfiliacionAsync(int empleadoId, int afiliacionId, TerminarAfiliacionComando comando, CancellationToken ct);
}

internal sealed class SegurosServicio(
    IPlanSeguroRepositorio planes,
    IAfiliacionSeguroRepositorio afiliaciones,
    IEmpleadoRepositorio empleados,
    IEmpleadoConsultas consultasEmpleados,
    ISegurosConsultas consultas,
    IUsuarioActual usuarioActual,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj) : ISegurosServicio
{
    public Task<IReadOnlyList<PlanSeguroDto>> ListarPlanesAsync(bool? activo, CancellationToken ct) =>
        consultas.ListarPlanesAsync(activo, ct);

    public async Task<PlanSeguroDto> ObtenerPlanAsync(int id, CancellationToken ct) =>
        await consultas.ObtenerPlanAsync(id, ct) ?? throw new RecursoNoEncontradoException("Plan de seguro", id);

    public async Task<PlanSeguroDto> CrearPlanAsync(GuardarPlanSeguroComando comando, CancellationToken ct)
    {
        usuarioActual.ExigirGestor();
        var plan = PlanSeguro.Crear(comando.Nombre, comando.Aseguradora, comando.Tipo, comando.PrimaMensualUf);
        await AsegurarNombreLibreAsync(plan, null, ct);

        planes.Agregar(plan);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerPlanAsync(plan.Id, ct);
    }

    public async Task<PlanSeguroDto> ActualizarPlanAsync(int id, GuardarPlanSeguroComando comando, CancellationToken ct)
    {
        usuarioActual.ExigirGestor();
        var plan = await ObtenerEntidadPlanAsync(id, ct);
        plan.Actualizar(comando.Nombre, comando.Aseguradora, comando.Tipo, comando.PrimaMensualUf);
        await AsegurarNombreLibreAsync(plan, id, ct);

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        return await ObtenerPlanAsync(id, ct);
    }

    public async Task CambiarEstadoPlanAsync(int id, bool activo, CancellationToken ct)
    {
        usuarioActual.ExigirGestor();
        var plan = await ObtenerEntidadPlanAsync(id, ct);
        if (activo)
        {
            plan.Activar();
        }
        else
        {
            plan.Desactivar();
        }

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
    }

    public async Task<IReadOnlyList<AfiliacionSeguroDto>> ListarAfiliacionesAsync(int empleadoId, CancellationToken ct)
    {
        await AsegurarEmpleadoAsync(empleadoId, ct);
        return await consultas.ListarAfiliacionesAsync(empleadoId, reloj.Hoy(), ct);
    }

    public async Task<AfiliacionSeguroDto> AfiliarAsync(int empleadoId, AfiliarSeguroComando comando, CancellationToken ct)
    {
        usuarioActual.ExigirGestor();
        await AsegurarEmpleadoAsync(empleadoId, ct);
        var empleado = await empleados.ObtenerAsync(empleadoId, ct)
            ?? throw new RecursoNoEncontradoException("Empleado", empleadoId);

        if (!empleado.Activo)
        {
            throw new ConflictoException("No se puede afiliar a un empleado desvinculado.");
        }

        var plan = await ObtenerEntidadPlanAsync(comando.PlanSeguroId, ct);
        if (!plan.Activo)
        {
            throw new ConflictoException("El plan de seguro no está activo.");
        }

        if (await afiliaciones.ExisteAfiliacionAbiertaAsync(empleadoId, plan.Id, ct))
        {
            throw new ConflictoException("El empleado ya está afiliado a este plan.");
        }

        var afiliacion = AfiliacionSeguro.Crear(empleadoId, plan.Id, comando.FechaInicio, comando.NumeroCargas);
        afiliaciones.Agregar(afiliacion);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerAfiliacionAsync(afiliacion.Id, ct);
    }

    public async Task<AfiliacionSeguroDto> TerminarAfiliacionAsync(
        int empleadoId, int afiliacionId, TerminarAfiliacionComando comando, CancellationToken ct)
    {
        usuarioActual.ExigirGestor();
        await AsegurarEmpleadoAsync(empleadoId, ct);
        // Se busca por empleado + id: evita modificar afiliaciones de otro empleado cambiando solo el id.
        var afiliacion = await afiliaciones.ObtenerAsync(empleadoId, afiliacionId, ct)
            ?? throw new RecursoNoEncontradoException("Afiliación", afiliacionId);

        afiliacion.Terminar(comando.FechaTermino);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerAfiliacionAsync(afiliacionId, ct);
    }

    private async Task<AfiliacionSeguroDto> ObtenerAfiliacionAsync(int id, CancellationToken ct) =>
        await consultas.ObtenerAfiliacionAsync(id, reloj.Hoy(), ct) ?? throw new RecursoNoEncontradoException("Afiliación", id);

    private async Task<PlanSeguro> ObtenerEntidadPlanAsync(int id, CancellationToken ct) =>
        await planes.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Plan de seguro", id);

    /// <summary>El empleado debe existir y ser visible para el usuario (si no, 404).</summary>
    private async Task AsegurarEmpleadoAsync(int empleadoId, CancellationToken ct)
    {
        if (!await consultasEmpleados.EstaEnAlcanceAsync(empleadoId, usuarioActual.Alcance(), ct))
        {
            throw new RecursoNoEncontradoException("Empleado", empleadoId);
        }
    }

    private async Task AsegurarNombreLibreAsync(PlanSeguro plan, int? excluirId, CancellationToken ct)
    {
        if (await planes.ExisteNombreAsync(plan.Nombre, plan.Aseguradora, excluirId, ct))
        {
            throw new ConflictoException($"Ya existe el plan '{plan.Nombre}' de {plan.Aseguradora}.");
        }
    }
}
