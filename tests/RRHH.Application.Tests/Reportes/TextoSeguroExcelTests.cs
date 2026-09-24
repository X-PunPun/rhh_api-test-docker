using RRHH.Application.Reportes;

namespace RRHH.Application.Tests.Reportes;

public class TextoSeguroExcelTests
{
    [Theory]
    [InlineData("=HYPERLINK(\"http://malo\")", "'=HYPERLINK(\"http://malo\")")]
    [InlineData("+56 9 1234 5678", "'+56 9 1234 5678")]
    [InlineData("-10", "'-10")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("\tTAB", "'\tTAB")]
    public void Neutralizar_TextoPeligroso_AnteponeApostrofo(string entrada, string esperado)
    {
        Assert.Equal(esperado, TextoSeguroExcel.Neutralizar(entrada));
    }

    [Theory]
    [InlineData("Juan Pérez")]
    [InlineData("12.345.678-5")]
    [InlineData("")]
    public void Neutralizar_TextoNormal_NoCambia(string entrada)
    {
        Assert.Equal(entrada, TextoSeguroExcel.Neutralizar(entrada));
    }

    [Fact]
    public void Neutralizar_Null_RetornaNull()
    {
        Assert.Null(TextoSeguroExcel.Neutralizar(null));
    }
}
