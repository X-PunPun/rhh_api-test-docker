namespace RRHH.Application.Comun;

/// <summary>Puerto para confirmar los cambios de un caso de uso en una sola transacción.</summary>
public interface IUnidadDeTrabajo
{
    Task GuardarCambiosAsync(CancellationToken ct);
}
