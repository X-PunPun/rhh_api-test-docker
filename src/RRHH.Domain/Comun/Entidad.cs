namespace RRHH.Domain.Comun;

/// <summary>Clase base para entidades con identidad.</summary>
public abstract class Entidad<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
}
