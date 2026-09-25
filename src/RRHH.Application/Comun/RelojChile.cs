namespace RRHH.Application.Comun;

/// <summary>
/// Reloj del sistema con la zona horaria de Chile continental (America/Santiago, con horario de verano).
/// Evita que "hoy" dependa de la zona del servidor: un contenedor Docker corre en UTC y entre las 20:00 y
/// las 24:00 de Chile ya sería "mañana". Las marcas de tiempo se siguen guardando en UTC.
/// </summary>
public sealed class RelojChile : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone { get; } = ObtenerZona();

    private static TimeZoneInfo ObtenerZona()
    {
        // IANA (Linux/macOS y Windows con ICU) y luego el identificador clásico de Windows.
        foreach (var id in new[] { "America/Santiago", "Pacific SA Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Local;
    }
}
