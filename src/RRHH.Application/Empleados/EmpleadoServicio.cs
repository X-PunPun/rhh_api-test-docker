using RRHH.Application.Comun;
using RRHH.Application.Organizacion;
using RRHH.Application.Ubicacion;
using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Application.Empleados;

public interface IEmpleadoServicio
{
    Task<Pagina<EmpleadoResumenDto>> BuscarAsync(FiltroEmpleados filtro, CancellationToken ct);
    Task<EmpleadoDetalleDto> ObtenerAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<EmpleadoResumenDto>> ListarSubordinadosAsync(int jefeId, CancellationToken ct);
    Task<EmpleadoDetalleDto> CrearAsync(CrearEmpleadoComando comando, CancellationToken ct);
    Task<EmpleadoDetalleDto> ActualizarAsync(int id, ActualizarEmpleadoComando comando, CancellationToken ct);
    Task DesvincularAsync(int id, DesvincularEmpleadoComando comando, CancellationToken ct);
}

internal sealed class EmpleadoServicio(
    IEmpleadoRepositorio empleados,
    IEmpleadoConsultas consultas,
    IDepartamentoRepositorio departamentos,
    ICargoRepositorio cargos,
    IUbicacionRepositorio ubicacion,
    IUnidadDeTrabajo unidadDeTrabajo) : IEmpleadoServicio
{
    /// <summary>Límite de seguridad al recorrer la jerarquía (evita bucles infinitos con datos corruptos).</summary>
    private const int ProfundidadMaximaJerarquia = 50;

    public Task<Pagina<EmpleadoResumenDto>> BuscarAsync(FiltroEmpleados filtro, CancellationToken ct) =>
        consultas.BuscarAsync(filtro, ct);

    public async Task<EmpleadoDetalleDto> ObtenerAsync(int id, CancellationToken ct) =>
        await consultas.ObtenerDetalleAsync(id, ct) ?? throw new RecursoNoEncontradoException("Empleado", id);

    public async Task<IReadOnlyList<EmpleadoResumenDto>> ListarSubordinadosAsync(int jefeId, CancellationToken ct)
    {
        _ = await ObtenerEntidadAsync(jefeId, ct);
        return await consultas.ListarSubordinadosAsync(jefeId, ct);
    }

    public async Task<EmpleadoDetalleDto> CrearAsync(CrearEmpleadoComando comando, CancellationToken ct)
    {
        var rut = Rut.Crear(comando.Rut);

        var empleado = Empleado.Crear(
            rut,
            comando.Nombres,
            comando.ApellidoPaterno,
            comando.ApellidoMaterno,
            comando.Email,
            comando.FechaNacimiento,
            comando.FechaIngreso,
            comando.DepartamentoId,
            comando.CargoId,
            comando.ComunaId,
            comando.Afp,
            comando.SistemaSalud,
            comando.AniosServicioPrevios);

        if (await empleados.ExisteRutAsync(rut, ct))
        {
            throw new ConflictoException($"Ya existe un empleado con RUT {rut.Formateado}.");
        }

        await AsegurarEmailLibreAsync(empleado.Email, null, ct);
        await ValidarAsignacionAsync(comando.DepartamentoId, comando.CargoId, ct);
        await ValidarComunaAsync(comando.ComunaId, ct);

        if (comando.JefeId is { } jefeId)
        {
            await ValidarJefeAsync(empleadoId: null, jefeId, ct);
            empleado.AsignarJefe(jefeId);
        }

        empleados.Agregar(empleado);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerAsync(empleado.Id, ct);
    }

    public async Task<EmpleadoDetalleDto> ActualizarAsync(int id, ActualizarEmpleadoComando comando, CancellationToken ct)
    {
        var empleado = await ObtenerEntidadAsync(id, ct);

        empleado.ActualizarContacto(comando.Email, comando.ComunaId);
        await AsegurarEmailLibreAsync(empleado.Email, id, ct);
        await ValidarComunaAsync(comando.ComunaId, ct);

        if (empleado.DepartamentoId != comando.DepartamentoId || empleado.CargoId != comando.CargoId)
        {
            await ValidarAsignacionAsync(comando.DepartamentoId, comando.CargoId, ct);
            empleado.CambiarAsignacion(comando.DepartamentoId, comando.CargoId);
        }

        if (empleado.JefeId != comando.JefeId)
        {
            if (comando.JefeId is { } jefeId)
            {
                await ValidarJefeAsync(id, jefeId, ct);
            }

            empleado.AsignarJefe(comando.JefeId);
        }

        empleado.ActualizarPrevision(comando.Afp, comando.SistemaSalud, comando.AniosServicioPrevios);

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task DesvincularAsync(int id, DesvincularEmpleadoComando comando, CancellationToken ct)
    {
        var empleado = await ObtenerEntidadAsync(id, ct);

        var subordinados = await consultas.ListarSubordinadosAsync(id, ct);
        if (subordinados.Any(s => s.Activo))
        {
            throw new ConflictoException(
                "El empleado tiene subordinados activos. Reasigne su jefatura antes de desvincularlo.");
        }

        empleado.Desvincular(comando.FechaTermino);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
    }

    private async Task<Empleado> ObtenerEntidadAsync(int id, CancellationToken ct) =>
        await empleados.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Empleado", id);

    private async Task AsegurarEmailLibreAsync(string email, int? excluirId, CancellationToken ct)
    {
        if (await empleados.ExisteEmailAsync(email, excluirId, ct))
        {
            throw new ConflictoException($"El email '{email}' ya está registrado.");
        }
    }

    private async Task ValidarAsignacionAsync(int departamentoId, int cargoId, CancellationToken ct)
    {
        var departamento = await departamentos.ObtenerAsync(departamentoId, ct)
            ?? throw new RecursoNoEncontradoException("Departamento", departamentoId);

        if (!departamento.Activo)
        {
            throw new ConflictoException("El departamento está inactivo.");
        }

        var cargo = await cargos.ObtenerAsync(cargoId, ct)
            ?? throw new RecursoNoEncontradoException("Cargo", cargoId);

        if (!cargo.Activo || cargo.DepartamentoId != departamentoId)
        {
            throw new ConflictoException("El cargo no está activo o no pertenece al departamento indicado.");
        }
    }

    private async Task ValidarComunaAsync(int comunaId, CancellationToken ct)
    {
        if (!await ubicacion.ExisteComunaAsync(comunaId, ct))
        {
            throw new RecursoNoEncontradoException("Comuna", comunaId);
        }
    }

    /// <summary>
    /// El jefe debe existir, estar activo y no generar un ciclo
    /// (ej.: A jefe de B y B jefe de A). Se recorre la cadena de mando hacia arriba.
    /// </summary>
    private async Task ValidarJefeAsync(int? empleadoId, int jefeId, CancellationToken ct)
    {
        var jefe = await empleados.ObtenerAsync(jefeId, ct)
            ?? throw new RecursoNoEncontradoException("Empleado (jefe)", jefeId);

        if (!jefe.Activo)
        {
            throw new ConflictoException("El jefe indicado no está activo.");
        }

        if (empleadoId is null)
        {
            return; // Un empleado nuevo no puede estar en la cadena de mando de nadie.
        }

        int? actual = jefeId;
        for (var nivel = 0; actual is not null && nivel < ProfundidadMaximaJerarquia; nivel++)
        {
            if (actual == empleadoId)
            {
                throw new ConflictoException("La jefatura indicada genera un ciclo en la jerarquía.");
            }

            actual = await empleados.ObtenerJefeIdAsync(actual.Value, ct);
        }
    }
}
