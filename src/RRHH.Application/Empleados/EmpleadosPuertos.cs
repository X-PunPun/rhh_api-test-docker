using RRHH.Application.Comun;
using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Application.Empleados;

public interface IEmpleadoRepositorio
{
    Task<Empleado?> ObtenerAsync(int id, CancellationToken ct);

    Task<bool> ExisteRutAsync(Rut rut, CancellationToken ct);

    Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct);

    /// <summary>Id del jefe directo de un empleado (null si no tiene o no existe).</summary>
    Task<int?> ObtenerJefeIdAsync(int empleadoId, CancellationToken ct);

    void Agregar(Empleado empleado);
}

public interface IEmpleadoConsultas
{
    Task<Pagina<EmpleadoResumenDto>> BuscarAsync(FiltroEmpleados filtro, CancellationToken ct);

    Task<EmpleadoDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<EmpleadoResumenDto>> ListarSubordinadosAsync(int jefeId, CancellationToken ct);

    /// <summary>Lista sin paginar para exportaciones (a lo más <paramref name="maximo"/> + 1 filas).</summary>
    Task<IReadOnlyList<EmpleadoResumenDto>> ListarParaExportarAsync(FiltroEmpleados filtro, int maximo, CancellationToken ct);
}
