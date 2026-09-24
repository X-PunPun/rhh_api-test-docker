using RRHH.Domain.Organizacion;

namespace RRHH.Application.Organizacion;

public interface IDepartamentoRepositorio
{
    Task<Departamento?> ObtenerAsync(int id, CancellationToken ct);

    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct);

    void Agregar(Departamento departamento);
}

public interface ICargoRepositorio
{
    Task<Cargo?> ObtenerAsync(int id, CancellationToken ct);

    Task<bool> ExisteNombreAsync(int departamentoId, string nombre, int? excluirId, CancellationToken ct);

    void Agregar(Cargo cargo);
}

/// <summary>Consultas de solo lectura (proyecciones directas a DTO).</summary>
public interface IOrganizacionConsultas
{
    Task<IReadOnlyList<DepartamentoDto>> ListarDepartamentosAsync(bool? activo, CancellationToken ct);

    Task<DepartamentoDto?> ObtenerDepartamentoAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<CargoDto>> ListarCargosAsync(int? departamentoId, bool? activo, CancellationToken ct);

    Task<CargoDto?> ObtenerCargoAsync(int id, CancellationToken ct);
}
