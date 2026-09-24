namespace RRHH.Application.Comun;

public static class RelojExtensions
{
    /// <summary>Fecha de hoy según el reloj inyectado (facilita pruebas con fechas fijas).</summary>
    public static DateOnly Hoy(this TimeProvider reloj) => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
}
