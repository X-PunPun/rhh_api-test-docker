using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Domain.Tests.Empleados;

public class EmpleadoTests
{
    private static readonly DateOnly FechaNacimiento = new(1990, 5, 20);
    private static readonly DateOnly FechaIngreso = new(2020, 3, 1);

    private static Empleado CrearEmpleado(
        DateOnly? fechaNacimiento = null,
        DateOnly? fechaIngreso = null,
        string email = "Juan.Perez@Empresa.cl",
        string nombres = "Juan") =>
        Empleado.Crear(
            Rut.Crear("12.345.678-5"),
            nombres,
            "Pérez",
            "Soto",
            email,
            fechaNacimiento ?? FechaNacimiento,
            fechaIngreso ?? FechaIngreso,
            departamentoId: 1,
            cargoId: 1,
            comunaId: 13101,
            Afp.Modelo,
            SistemaSalud.Fonasa);

    [Fact]
    public void Crear_DatosValidos_EmpleadoActivoConEmailNormalizado()
    {
        var empleado = CrearEmpleado();

        Assert.True(empleado.Activo);
        Assert.Equal("juan.perez@empresa.cl", empleado.Email);
        Assert.Equal("Juan Pérez Soto", empleado.NombreCompleto);
    }

    [Fact]
    public void Crear_MenorDeEdadAlIngreso_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() =>
            CrearEmpleado(fechaNacimiento: new DateOnly(2010, 1, 1), fechaIngreso: new DateOnly(2026, 1, 1)));
    }

    [Theory]
    [InlineData("sin-arroba.cl")]
    [InlineData("@empresa.cl")]
    [InlineData("juan@")]
    [InlineData("juan@empresa")]
    [InlineData("juan@@empresa.cl")]
    public void Crear_EmailInvalido_LanzaExcepcion(string email)
    {
        Assert.Throws<ExcepcionDominio>(() => CrearEmpleado(email: email));
    }

    [Fact]
    public void Crear_NombreVacio_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => CrearEmpleado(nombres: "  "));
    }

    [Fact]
    public void Desvincular_FechaAnteriorAlIngreso_LanzaExcepcion()
    {
        var empleado = CrearEmpleado();

        Assert.Throws<ExcepcionDominio>(() => empleado.Desvincular(FechaIngreso.AddDays(-1)));
    }

    [Fact]
    public void Desvincular_DosVeces_LanzaExcepcion()
    {
        var empleado = CrearEmpleado();
        empleado.Desvincular(new DateOnly(2025, 12, 31));

        Assert.False(empleado.Activo);
        Assert.Throws<ExcepcionDominio>(() => empleado.Desvincular(new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void CambiarAsignacion_EmpleadoDesvinculado_LanzaExcepcion()
    {
        var empleado = CrearEmpleado();
        empleado.Desvincular(new DateOnly(2025, 12, 31));

        Assert.Throws<ExcepcionDominio>(() => empleado.CambiarAsignacion(2, 2));
    }

    [Theory]
    [InlineData("2021-02-28", 0)]   // un día antes de cumplir 1 año
    [InlineData("2021-03-01", 1)]   // cumple exactamente 1 año
    [InlineData("2030-06-15", 10)]
    [InlineData("2019-01-01", 0)]   // fecha anterior al ingreso
    public void AniosDeServicio_CalculaAniosCompletos(string aLaFecha, int esperado)
    {
        var empleado = CrearEmpleado();

        Assert.Equal(esperado, empleado.AniosDeServicio(DateOnly.Parse(aLaFecha)));
    }

    [Fact]
    public void AniosDeServicio_EmpleadoDesvinculado_SeCongelaEnFechaTermino()
    {
        var empleado = CrearEmpleado();
        empleado.Desvincular(new DateOnly(2023, 3, 1));

        Assert.Equal(3, empleado.AniosDeServicio(new DateOnly(2030, 1, 1)));
    }

    [Fact]
    public void Crear_AfpInvalida_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => Empleado.Crear(
            Rut.Crear("11.111.111-1"), "Ana", "Rojas", null, "ana@empresa.cl",
            FechaNacimiento, FechaIngreso, 1, 1, 1, (Afp)99, SistemaSalud.Fonasa));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(61)]
    public void ActualizarPrevision_AniosPreviosFueraDeRango_LanzaExcepcion(int anios)
    {
        var empleado = CrearEmpleado();

        Assert.Throws<ExcepcionDominio>(() => empleado.ActualizarPrevision(Afp.Capital, SistemaSalud.Isapre, anios));
    }

    [Fact]
    public void AsignarJefe_IdInvalido_LanzaExcepcion()
    {
        var empleado = CrearEmpleado();

        Assert.Throws<ExcepcionDominio>(() => empleado.AsignarJefe(0));
    }

    [Fact]
    public void AsignarJefe_Null_QuitaJefatura()
    {
        var empleado = CrearEmpleado();
        empleado.AsignarJefe(5);
        empleado.AsignarJefe(null);

        Assert.Null(empleado.JefeId);
    }
}
