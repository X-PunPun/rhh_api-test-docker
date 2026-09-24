using RRHH.Application.Comun;
using RRHH.Domain.Organizacion;

namespace RRHH.Application.Organizacion;

public interface IOrganizacionServicio
{
    Task<IReadOnlyList<DepartamentoDto>> ListarDepartamentosAsync(bool? activo, CancellationToken ct);
    Task<DepartamentoDto> ObtenerDepartamentoAsync(int id, CancellationToken ct);
    Task<DepartamentoDto> CrearDepartamentoAsync(GuardarDepartamentoComando comando, CancellationToken ct);
    Task<DepartamentoDto> ActualizarDepartamentoAsync(int id, GuardarDepartamentoComando comando, CancellationToken ct);
    Task CambiarEstadoDepartamentoAsync(int id, bool activo, CancellationToken ct);

    Task<IReadOnlyList<CargoDto>> ListarCargosAsync(int? departamentoId, bool? activo, CancellationToken ct);
    Task<CargoDto> ObtenerCargoAsync(int id, CancellationToken ct);
    Task<CargoDto> CrearCargoAsync(CrearCargoComando comando, CancellationToken ct);
    Task<CargoDto> RenombrarCargoAsync(int id, RenombrarCargoComando comando, CancellationToken ct);
    Task DesactivarCargoAsync(int id, CancellationToken ct);
}

internal sealed class OrganizacionServicio(
    IDepartamentoRepositorio departamentos,
    ICargoRepositorio cargos,
    IOrganizacionConsultas consultas,
    IUnidadDeTrabajo unidadDeTrabajo) : IOrganizacionServicio
{
    public Task<IReadOnlyList<DepartamentoDto>> ListarDepartamentosAsync(bool? activo, CancellationToken ct) =>
        consultas.ListarDepartamentosAsync(activo, ct);

    public async Task<DepartamentoDto> ObtenerDepartamentoAsync(int id, CancellationToken ct) =>
        await consultas.ObtenerDepartamentoAsync(id, ct) ?? throw new RecursoNoEncontradoException("Departamento", id);

    public async Task<DepartamentoDto> CrearDepartamentoAsync(GuardarDepartamentoComando comando, CancellationToken ct)
    {
        var departamento = Departamento.Crear(comando.Nombre, comando.Descripcion);
        await AsegurarNombreDepartamentoLibreAsync(departamento.Nombre, null, ct);

        departamentos.Agregar(departamento);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerDepartamentoAsync(departamento.Id, ct);
    }

    public async Task<DepartamentoDto> ActualizarDepartamentoAsync(int id, GuardarDepartamentoComando comando, CancellationToken ct)
    {
        var departamento = await ObtenerEntidadDepartamentoAsync(id, ct);
        departamento.Actualizar(comando.Nombre, comando.Descripcion);
        await AsegurarNombreDepartamentoLibreAsync(departamento.Nombre, id, ct);

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        return await ObtenerDepartamentoAsync(id, ct);
    }

    public async Task CambiarEstadoDepartamentoAsync(int id, bool activo, CancellationToken ct)
    {
        var departamento = await ObtenerEntidadDepartamentoAsync(id, ct);

        if (!activo)
        {
            var dto = await ObtenerDepartamentoAsync(id, ct);
            if (dto.EmpleadosActivos > 0)
            {
                throw new ConflictoException(
                    $"No se puede desactivar: el departamento tiene {dto.EmpleadosActivos} empleado(s) activo(s).");
            }

            departamento.Desactivar();
        }
        else
        {
            departamento.Activar();
        }

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
    }

    public Task<IReadOnlyList<CargoDto>> ListarCargosAsync(int? departamentoId, bool? activo, CancellationToken ct) =>
        consultas.ListarCargosAsync(departamentoId, activo, ct);

    public async Task<CargoDto> ObtenerCargoAsync(int id, CancellationToken ct) =>
        await consultas.ObtenerCargoAsync(id, ct) ?? throw new RecursoNoEncontradoException("Cargo", id);

    public async Task<CargoDto> CrearCargoAsync(CrearCargoComando comando, CancellationToken ct)
    {
        var departamento = await ObtenerEntidadDepartamentoAsync(comando.DepartamentoId, ct);
        if (!departamento.Activo)
        {
            throw new ConflictoException("No se pueden crear cargos en un departamento inactivo.");
        }

        var cargo = Cargo.Crear(comando.Nombre, comando.DepartamentoId);
        await AsegurarNombreCargoLibreAsync(cargo.DepartamentoId, cargo.Nombre, null, ct);

        cargos.Agregar(cargo);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return await ObtenerCargoAsync(cargo.Id, ct);
    }

    public async Task<CargoDto> RenombrarCargoAsync(int id, RenombrarCargoComando comando, CancellationToken ct)
    {
        var cargo = await cargos.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Cargo", id);
        cargo.Renombrar(comando.Nombre);
        await AsegurarNombreCargoLibreAsync(cargo.DepartamentoId, cargo.Nombre, id, ct);

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        return await ObtenerCargoAsync(id, ct);
    }

    public async Task DesactivarCargoAsync(int id, CancellationToken ct)
    {
        var cargo = await cargos.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Cargo", id);
        cargo.Desactivar();
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
    }

    private async Task<Departamento> ObtenerEntidadDepartamentoAsync(int id, CancellationToken ct) =>
        await departamentos.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Departamento", id);

    private async Task AsegurarNombreDepartamentoLibreAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        if (await departamentos.ExisteNombreAsync(nombre, excluirId, ct))
        {
            throw new ConflictoException($"Ya existe un departamento llamado '{nombre}'.");
        }
    }

    private async Task AsegurarNombreCargoLibreAsync(int departamentoId, string nombre, int? excluirId, CancellationToken ct)
    {
        if (await cargos.ExisteNombreAsync(departamentoId, nombre, excluirId, ct))
        {
            throw new ConflictoException($"Ya existe el cargo '{nombre}' en ese departamento.");
        }
    }
}
