using RRHH.Domain.Comun;

namespace RRHH.Domain.Seguros;

/// <summary>Plan de seguro complementario ofrecido por la empresa.</summary>
public sealed class PlanSeguro : Entidad<int>
{
    public const int LargoMaximoNombre = 100;

    public string Nombre { get; private set; } = null!;
    public string Aseguradora { get; private set; } = null!;
    public TipoSeguro Tipo { get; private set; }

    /// <summary>Prima mensual por titular expresada en UF.</summary>
    public decimal PrimaMensualUf { get; private set; }

    public bool Activo { get; private set; }

    private PlanSeguro() { } // EF Core

    public static PlanSeguro Crear(string nombre, string aseguradora, TipoSeguro tipo, decimal primaMensualUf)
    {
        var plan = new PlanSeguro { Activo = true };
        plan.Actualizar(nombre, aseguradora, tipo, primaMensualUf);
        return plan;
    }

    public void Actualizar(string nombre, string aseguradora, TipoSeguro tipo, decimal primaMensualUf)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new ExcepcionDominio("El tipo de seguro no es válido.");
        }

        if (primaMensualUf is < 0 or > 1000)
        {
            throw new ExcepcionDominio("La prima mensual debe estar entre 0 y 1.000 UF.");
        }

        Nombre = Guardia.TextoRequerido(nombre, nameof(Nombre), LargoMaximoNombre);
        Aseguradora = Guardia.TextoRequerido(aseguradora, nameof(Aseguradora), LargoMaximoNombre);
        Tipo = tipo;
        PrimaMensualUf = decimal.Round(primaMensualUf, 4);
    }

    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;
}
