namespace RRHH.Domain.Vacaciones;

/// <summary>
/// Reglas de feriado legal (Código del Trabajo de Chile, simplificadas para el proyecto):
/// <list type="bullet">
/// <item>Art. 67: 15 días hábiles por año → se devengan 1,25 días por mes completo trabajado.</item>
/// <item>Art. 68: feriado progresivo, +1 día por cada 3 años sobre 10 (máx. 10 años con empleadores anteriores).</item>
/// <item>Art. 69: el sábado se considera inhábil; tampoco se cuentan domingos ni feriados.</item>
/// </list>
/// </summary>
public static class CalculadoraVacaciones
{
    public const decimal DiasBaseAnuales = 15m;
    public const decimal DiasPorMes = DiasBaseAnuales / 12m; // 1,25
    public const int AniosParaProgresivo = 10;
    public const int AniosPorDiaProgresivo = 3;
    public const int MaximoAniosPrevios = 10;

    /// <summary>Días hábiles entre dos fechas (ambas inclusive).</summary>
    public static int ContarDiasHabiles(DateOnly inicio, DateOnly fin, IReadOnlySet<DateOnly> feriados)
    {
        ArgumentNullException.ThrowIfNull(feriados);

        var dias = 0;
        for (var fecha = inicio; fecha <= fin; fecha = fecha.AddDays(1))
        {
            if (fecha.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !feriados.Contains(fecha))
            {
                dias++;
            }
        }

        return dias;
    }

    /// <summary>Días adicionales por feriado progresivo para un año de servicio dado.</summary>
    public static int DiasProgresivos(int aniosPrevios, int aniosEnEmpresa)
    {
        var total = Math.Clamp(aniosPrevios, 0, MaximoAniosPrevios) + Math.Max(aniosEnEmpresa, 0);
        return total > AniosParaProgresivo ? (total - AniosParaProgresivo) / AniosPorDiaProgresivo : 0;
    }

    /// <summary>
    /// Días devengados desde el ingreso: 1,25 por mes completo + días progresivos
    /// que se ganan al cumplir cada aniversario.
    /// </summary>
    public static decimal DiasDevengados(DateOnly fechaIngreso, DateOnly aLaFecha, int aniosPrevios)
    {
        if (aLaFecha < fechaIngreso)
        {
            return 0m;
        }

        var meses = MesesCompletos(fechaIngreso, aLaFecha);
        var aniosCompletos = meses / 12;

        var progresivos = 0;
        for (var anio = 1; anio <= aniosCompletos; anio++)
        {
            progresivos += DiasProgresivos(aniosPrevios, anio);
        }

        return meses * DiasPorMes + progresivos;
    }

    public static int MesesCompletos(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde)
        {
            return 0;
        }

        var meses = (hasta.Year - desde.Year) * 12 + hasta.Month - desde.Month;
        if (hasta < desde.AddMonths(meses))
        {
            meses--;
        }

        return meses;
    }
}
