using RRHH.Domain.Comun;
using RRHH.Domain.Seguros;

namespace RRHH.Domain.Tests.Seguros;

public class SegurosTests
{
    [Fact]
    public void PlanSeguro_PrimaNegativa_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => PlanSeguro.Crear("Plan", "Aseguradora", TipoSeguro.Salud, -1m));
    }

    [Fact]
    public void PlanSeguro_RedondeaPrimaA4Decimales()
    {
        var plan = PlanSeguro.Crear("Plan", "Aseguradora", TipoSeguro.Vida, 0.123456m);

        Assert.Equal(0.1235m, plan.PrimaMensualUf);
        Assert.True(plan.Activo);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(AfiliacionSeguro.MaximoCargas + 1)]
    public void Afiliacion_CargasFueraDeRango_LanzaExcepcion(int cargas)
    {
        Assert.Throws<ExcepcionDominio>(() => AfiliacionSeguro.Crear(1, 1, new DateOnly(2026, 1, 1), cargas));
    }

    [Fact]
    public void Afiliacion_VigenciaSegunFechas()
    {
        var afiliacion = AfiliacionSeguro.Crear(1, 1, new DateOnly(2026, 1, 1), 2);
        afiliacion.Terminar(new DateOnly(2026, 6, 30));

        Assert.False(afiliacion.EstaVigente(new DateOnly(2025, 12, 31)));
        Assert.True(afiliacion.EstaVigente(new DateOnly(2026, 6, 30)));
        Assert.False(afiliacion.EstaVigente(new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void Afiliacion_TerminarDosVeces_LanzaExcepcion()
    {
        var afiliacion = AfiliacionSeguro.Crear(1, 1, new DateOnly(2026, 1, 1), 0);
        afiliacion.Terminar(new DateOnly(2026, 6, 30));

        Assert.Throws<ExcepcionDominio>(() => afiliacion.Terminar(new DateOnly(2026, 7, 31)));
    }
}
