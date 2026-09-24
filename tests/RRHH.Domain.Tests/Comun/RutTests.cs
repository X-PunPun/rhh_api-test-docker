using RRHH.Domain.Comun;

namespace RRHH.Domain.Tests.Comun;

public class RutTests
{
    [Theory]
    [InlineData("11.111.111-1", 11111111, '1')]
    [InlineData("12345678-5", 12345678, '5')]
    [InlineData("123456785", 12345678, '5')]
    [InlineData("10.000.013-k", 10000013, 'K')]   // DV 'K' en minúscula
    [InlineData("10000004-0", 10000004, '0')]     // DV '0'
    [InlineData(" 7.654.321-6 ", 7654321, '6')]    // espacios alrededor
    public void TryParse_RutValido_RetornaRutNormalizado(string entrada, int numero, char dv)
    {
        var ok = Rut.TryParse(entrada, out var rut);

        Assert.True(ok);
        Assert.NotNull(rut);
        Assert.Equal(numero, rut.Numero);
        Assert.Equal(dv, rut.DigitoVerificador);
    }

    [Theory]
    [InlineData((string?)null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12.345.678-9")]   // DV incorrecto
    [InlineData("12345678-X")]     // DV no numérico ni K
    [InlineData("1234567890-1")]   // demasiado largo
    [InlineData("0-0")]            // número cero
    [InlineData("ABCDEFGH-1")]     // no numérico
    [InlineData("-5")]
    public void TryParse_RutInvalido_RetornaFalso(string? entrada)
    {
        Assert.False(Rut.TryParse(entrada, out var rut));
        Assert.Null(rut);
    }

    [Fact]
    public void Crear_RutInvalido_LanzaExcepcionDominio()
    {
        Assert.Throws<ExcepcionDominio>(() => Rut.Crear("12.345.678-9"));
    }

    [Fact]
    public void Formatos_SonConsistentes()
    {
        var rut = Rut.Crear("12345678-5");

        Assert.Equal("12345678-5", rut.Valor);
        Assert.Equal("12.345.678-5", rut.Formateado);
        Assert.Equal("12345678-5", rut.ToString());
    }

    [Fact]
    public void DosRutConMismoValor_SonIguales()
    {
        Assert.Equal(Rut.Crear("12.345.678-5"), Rut.Crear("123456785"));
    }

    [Theory]
    [InlineData(11111111, '1')]
    [InlineData(12345678, '5')]
    [InlineData(10000013, 'K')]
    [InlineData(10000004, '0')]
    public void CalcularDigitoVerificador_Modulo11(int numero, char esperado)
    {
        Assert.Equal(esperado, Rut.CalcularDigitoVerificador(numero));
    }
}
