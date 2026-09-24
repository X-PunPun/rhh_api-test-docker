using RRHH.Domain.Comun;

namespace RRHH.Domain.Ubicacion;

/// <summary>Comuna de Chile. El Id corresponde al Código Único Territorial (CUT).</summary>
public sealed class Comuna : Entidad<int>
{
    public string Nombre { get; private set; } = null!;
    public int RegionId { get; private set; }
    public Region Region { get; private set; } = null!;

    private Comuna() { } // EF Core

    public static Comuna Crear(int id, string nombre, int regionId)
    {
        return new Comuna
        {
            Id = Guardia.IdPositivo(id, nameof(Id)),
            Nombre = Guardia.TextoRequerido(nombre, nameof(Nombre), 100),
            RegionId = Guardia.IdPositivo(regionId, nameof(RegionId)),
        };
    }
}
