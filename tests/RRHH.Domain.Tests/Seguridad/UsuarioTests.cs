using RRHH.Domain.Comun;
using RRHH.Domain.Seguridad;

namespace RRHH.Domain.Tests.Seguridad;

public class UsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private static Usuario Empleado(Rol rol = Rol.Empleado, int[]? regiones = null) =>
        Usuario.Crear("Ana.Rojas@Empresa.cl", "hash", rol, 10, regiones, Ahora);

    [Fact]
    public void Crear_NormalizaEmailYQuedaActivo()
    {
        var usuario = Empleado();

        Assert.Equal("ana.rojas@empresa.cl", usuario.Email);
        Assert.True(usuario.Activo);
        Assert.False(string.IsNullOrEmpty(usuario.SelloSeguridad));
    }

    [Fact]
    public void RRHH_SinRegiones_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => Empleado(Rol.RRHH));
    }

    [Fact]
    public void Empleado_ConRegiones_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => Empleado(Rol.Empleado, [13]));
    }

    [Fact]
    public void Jefatura_SinEmpleadoAsociado_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => Usuario.Crear("jefe@empresa.cl", "hash", Rol.Jefatura, null, null, Ahora));
    }

    [Fact]
    public void RegionInvalida_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => Empleado(Rol.RRHH, [17]));
    }

    [Fact]
    public void RRHH_RegionesSinDuplicadosYOrdenadas()
    {
        var usuario = Empleado(Rol.RRHH, [13, 5, 13]);

        Assert.Equal(new[] { 5, 13 }, usuario.Regiones);
    }

    [Fact]
    public void CincoIntentosFallidos_BloqueanQuinceMinutos()
    {
        var usuario = Empleado();

        for (var i = 0; i < Usuario.IntentosAntesDeBloqueo - 1; i++)
        {
            usuario.RegistrarAccesoFallido(Ahora);
            Assert.False(usuario.EstaBloqueado(Ahora));
        }

        usuario.RegistrarAccesoFallido(Ahora);

        Assert.True(usuario.EstaBloqueado(Ahora.AddMinutes(14)));
        Assert.False(usuario.EstaBloqueado(Ahora.AddMinutes(16)));
    }

    [Fact]
    public void AccesoExitoso_ReiniciaIntentos()
    {
        var usuario = Empleado();
        usuario.RegistrarAccesoFallido(Ahora);
        usuario.RegistrarAccesoFallido(Ahora);

        usuario.RegistrarAccesoExitoso(Ahora);

        Assert.Equal(0, usuario.IntentosFallidos);
        Assert.Equal(Ahora, usuario.UltimoAcceso);
    }

    [Fact]
    public void CambiarClave_RenuevaSelloDeSeguridad()
    {
        var usuario = Empleado();
        var selloAnterior = usuario.SelloSeguridad;

        usuario.CambiarClave("nuevo-hash");

        Assert.NotEqual(selloAnterior, usuario.SelloSeguridad);
    }

    [Fact]
    public void TokenRenovacion_RevocadoOExpirado_NoEstaVigente()
    {
        var token = TokenRenovacion.Crear(1, "hash", "sello", Ahora, TimeSpan.FromDays(7));

        Assert.True(token.EstaVigente(Ahora.AddDays(6)));
        Assert.False(token.EstaVigente(Ahora.AddDays(8)));

        token.Revocar(Ahora, "logout");
        Assert.False(token.EstaVigente(Ahora));
        Assert.Equal("logout", token.MotivoRevocacion);
    }
}
