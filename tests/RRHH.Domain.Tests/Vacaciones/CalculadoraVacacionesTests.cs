using RRHH.Domain.Vacaciones;

namespace RRHH.Domain.Tests.Vacaciones;

public class CalculadoraVacacionesTests
{
    private static readonly IReadOnlySet<DateOnly> SinFeriados = new HashSet<DateOnly>();

    [Fact]
    public void ContarDiasHabiles_SemanaCompleta_CuentaLunesAViernes()
    {
        // Lunes 2 al domingo 8 de noviembre de 2026
        var dias = CalculadoraVacaciones.ContarDiasHabiles(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 8), SinFeriados);

        Assert.Equal(5, dias);
    }

    [Fact]
    public void ContarDiasHabiles_DescuentaFeriados()
    {
        // Semana del 14 al 18 de septiembre de 2026, con el 18 feriado (viernes)
        var feriados = new HashSet<DateOnly> { new(2026, 9, 18) };

        var dias = CalculadoraVacaciones.ContarDiasHabiles(new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 18), feriados);

        Assert.Equal(4, dias);
    }

    [Fact]
    public void ContarDiasHabiles_SoloFinDeSemana_EsCero()
    {
        var dias = CalculadoraVacaciones.ContarDiasHabiles(new DateOnly(2026, 11, 7), new DateOnly(2026, 11, 8), SinFeriados);

        Assert.Equal(0, dias);
    }

    [Theory]
    [InlineData(0, 10, 0)]   // 10 años: aún no hay progresivo
    [InlineData(0, 12, 0)]   // 12 años: faltan 3 nuevos años
    [InlineData(0, 13, 1)]   // 13 años: +1
    [InlineData(10, 3, 1)]   // 10 previos + 3 en la empresa: +1
    [InlineData(15, 3, 1)]   // previos se topan en 10
    [InlineData(10, 9, 3)]   // 10 + 9 = 19 → (19 - 10) / 3 = 3
    public void DiasProgresivos_Art68(int previos, int enEmpresa, int esperado)
    {
        Assert.Equal(esperado, CalculadoraVacaciones.DiasProgresivos(previos, enEmpresa));
    }

    [Fact]
    public void DiasDevengados_UnAnioSinProgresivo_Son15()
    {
        var devengados = CalculadoraVacaciones.DiasDevengados(new DateOnly(2025, 3, 1), new DateOnly(2026, 3, 1), 0);

        Assert.Equal(15m, devengados);
    }

    [Fact]
    public void DiasDevengados_MesesIncompletosNoSuman()
    {
        // 5 meses completos (1-mar a 31-ago) → 6,25 días
        var devengados = CalculadoraVacaciones.DiasDevengados(new DateOnly(2026, 3, 1), new DateOnly(2026, 8, 31), 0);

        Assert.Equal(6.25m, devengados);
    }

    [Fact]
    public void DiasDevengados_IncluyeProgresivoAlCumplirAniversario()
    {
        // 10 años previos, 3 años en la empresa: 36 meses × 1,25 = 45 + 1 progresivo (año 3)
        var devengados = CalculadoraVacaciones.DiasDevengados(new DateOnly(2023, 1, 1), new DateOnly(2026, 1, 1), 10);

        Assert.Equal(46m, devengados);
    }

    [Fact]
    public void DiasDevengados_FechaAnteriorAlIngreso_EsCero()
    {
        Assert.Equal(0m, CalculadoraVacaciones.DiasDevengados(new DateOnly(2026, 1, 1), new DateOnly(2025, 1, 1), 0));
    }
}
