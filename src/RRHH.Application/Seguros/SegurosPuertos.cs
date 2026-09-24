using RRHH.Domain.Seguros;

namespace RRHH.Application.Seguros;

public interface IPlanSeguroRepositorio
{
    Task<PlanSeguro?> ObtenerAsync(int id, CancellationToken ct);

    Task<bool> ExisteNombreAsync(string nombre, string aseguradora, int? excluirId, CancellationToken ct);

    void Agregar(PlanSeguro plan);
}

public interface IAfiliacionSeguroRepositorio
{
    Task<AfiliacionSeguro?> ObtenerAsync(int empleadoId, int afiliacionId, CancellationToken ct);

    /// <summary>¿El empleado ya tiene una afiliación sin término al mismo plan?</summary>
    Task<bool> ExisteAfiliacionAbiertaAsync(int empleadoId, int planSeguroId, CancellationToken ct);

    void Agregar(AfiliacionSeguro afiliacion);
}

public interface ISegurosConsultas
{
    Task<IReadOnlyList<PlanSeguroDto>> ListarPlanesAsync(bool? activo, CancellationToken ct);

    Task<PlanSeguroDto?> ObtenerPlanAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<AfiliacionSeguroDto>> ListarAfiliacionesAsync(int empleadoId, DateOnly hoy, CancellationToken ct);

    Task<AfiliacionSeguroDto?> ObtenerAfiliacionAsync(int afiliacionId, DateOnly hoy, CancellationToken ct);
}
