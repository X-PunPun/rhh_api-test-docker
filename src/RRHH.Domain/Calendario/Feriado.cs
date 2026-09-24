using RRHH.Domain.Comun;

namespace RRHH.Domain.Calendario;

/// <summary>Día feriado legal en Chile (no se descuenta de vacaciones).</summary>
public sealed class Feriado : Entidad<int>
{
    public const int LargoMaximoNombre = 150;

    public DateOnly Fecha { get; private set; }
    public string Nombre { get; private set; } = null!;

    private Feriado() { } // EF Core

    public static Feriado Crear(DateOnly fecha, string nombre)
    {
        return new Feriado
        {
            Fecha = fecha,
            Nombre = Guardia.TextoRequerido(nombre, nameof(Nombre), LargoMaximoNombre),
        };
    }
}
