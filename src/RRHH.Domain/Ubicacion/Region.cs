using RRHH.Domain.Comun;

namespace RRHH.Domain.Ubicacion;

/// <summary>Región de Chile. El Id corresponde al código CUT de la región (1..16).</summary>
public sealed class Region : Entidad<int>
{
    private readonly List<Comuna> _comunas = [];

    public string Nombre { get; private set; } = null!;

    /// <summary>Numeral romano o "RM" (ej.: "XV", "RM").</summary>
    public string Abreviatura { get; private set; } = null!;

    /// <summary>Orden geográfico de norte a sur.</summary>
    public int Orden { get; private set; }

    public IReadOnlyCollection<Comuna> Comunas => _comunas.AsReadOnly();

    private Region() { } // EF Core

    public static Region Crear(int id, string nombre, string abreviatura, int orden)
    {
        return new Region
        {
            Id = Guardia.IdPositivo(id, nameof(Id)),
            Nombre = Guardia.TextoRequerido(nombre, nameof(Nombre), 100),
            Abreviatura = Guardia.TextoRequerido(abreviatura, nameof(Abreviatura), 5),
            Orden = orden,
        };
    }
}
