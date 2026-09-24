using RRHH.Application.Comun;
using RRHH.Application.Seguridad;
using RRHH.Domain.Comun;
using RRHH.Domain.Seguridad;

namespace RRHH.Application.Tests.Seguridad;

public class PoliticaYAlcanceTests
{
    [Theory]
    [InlineData("corta1!A")]            // largo
    [InlineData("sinmayuscula1!")]      // mayúscula
    [InlineData("SINMINUSCULA1!")]      // minúscula
    [InlineData("SinNumero!!!!")]       // número
    [InlineData("SinSimbolo1234")]      // símbolo
    [InlineData("Ana.rojas2026!")]      // contiene el usuario
    public void PoliticaClaves_RechazaClavesDebiles(string clave)
    {
        Assert.Throws<ExcepcionDominio>(() => PoliticaClaves.Validar(clave, "ana.rojas@empresa.cl"));
    }

    [Fact]
    public void PoliticaClaves_AceptaClaveFuerte()
    {
        PoliticaClaves.Validar("Tr3s.Volcanes!", "ana.rojas@empresa.cl");
    }

    [Fact]
    public void Alcance_Admin_EsTotal()
    {
        Assert.True(new Usuario(Rol.Admin).Alcance().Total);
    }

    [Fact]
    public void Alcance_RRHH_SusRegionesYSuFicha()
    {
        var alcance = new Usuario(Rol.RRHH, empleadoId: 3, regiones: [5, 13]).Alcance();

        Assert.False(alcance.Total);
        Assert.Equal(new[] { 5, 13 }, alcance.Regiones);
        Assert.Equal(3, alcance.EmpleadoPropioId);
        Assert.Null(alcance.JefeId);
    }

    [Fact]
    public void Alcance_Jefatura_SuEquipoYSuFicha()
    {
        var alcance = new Usuario(Rol.Jefatura, empleadoId: 5).Alcance();

        Assert.Equal(5, alcance.EmpleadoPropioId);
        Assert.Equal(5, alcance.JefeId);
        Assert.Empty(alcance.Regiones);
    }

    [Fact]
    public void Alcance_Empleado_SoloSuFicha()
    {
        var alcance = new Usuario(Rol.Empleado, empleadoId: 7).Alcance();

        Assert.Equal(7, alcance.EmpleadoPropioId);
        Assert.Null(alcance.JefeId);
    }

    [Fact]
    public void Alcance_SinSesion_NoVeNada()
    {
        Assert.Equal(AlcanceDatos.Ninguno, new Usuario(null).Alcance());
    }

    [Theory]
    [InlineData(Rol.Admin, 9, true)]
    [InlineData(Rol.RRHH, 13, true)]
    [InlineData(Rol.RRHH, 9, false)]
    [InlineData(Rol.Jefatura, 13, false)]
    public void PuedeGestionarRegion(Rol rol, int region, bool esperado)
    {
        var usuario = new Usuario(rol, empleadoId: 1, regiones: rol == Rol.RRHH ? [13] : []);

        Assert.Equal(esperado, usuario.PuedeGestionarRegion(region));
    }

    private sealed class Usuario(Rol? rol, int? empleadoId = null, IReadOnlyList<int>? regiones = null) : IUsuarioActual
    {
        public int? UsuarioId => rol is null ? null : 1;
        public int? EmpleadoId => empleadoId;
        public string? Email => null;
        public Rol? Rol => rol;
        public IReadOnlyList<int> Regiones => regiones ?? [];
        public string? Ip => null;
    }
}
