using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Seguridad;
using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;
using RRHH.Domain.Seguridad;

namespace RRHH.Application.Tests.Seguridad;

public class AutenticacionServicioTests
{
    private const string Clave = "Clave.Segura2026";
    private static readonly DateTimeOffset Ahora = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly Usuarios _usuarios = new();
    private readonly Tokens _tokens = new();
    private readonly Empleados _empleados = new();
    private readonly Reloj _reloj = new(Ahora);

    public AutenticacionServicioTests()
    {
        var empleado = Empleado.Crear(Rut.Crear("12.345.678-5"), "Ana", "Rojas", null, "ana@empresa.cl",
            new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1), 1, 1, 13101, Afp.Modelo, SistemaSalud.Fonasa);
        EstablecerId(empleado, 10);
        _empleados.Datos[10] = empleado;

        var usuario = Usuario.Crear("ana@empresa.cl", Hasher.Prefijo + Clave, Rol.Empleado, 10, null, Ahora);
        EstablecerId(usuario, 1);
        _usuarios.Datos.Add(usuario);
    }

    private AutenticacionServicio Servicio() => new(
        _usuarios, _tokens, _empleados, new Generador(), new Hasher(), new AuditoriaNula(), new SinUsuario(), new UnidadNula(), _reloj);

    private Task<TokensDto> Login(string clave = Clave, string email = "ANA@empresa.cl") =>
        Servicio().IniciarSesionAsync(new LoginComando(email, clave), CancellationToken.None);

    [Fact]
    public async Task Login_Correcto_EntregaTokensYPerfil()
    {
        var tokens = await Login();

        Assert.StartsWith("jwt-1-", tokens.TokenAcceso);
        Assert.Equal(Rol.Empleado, tokens.Usuario.Rol);
        Assert.Equal("Ana Rojas", tokens.Usuario.Nombre);
        Assert.Single(_tokens.Datos); // se guardó el token de renovación (hasheado)
        Assert.NotEqual(tokens.TokenRenovacion, _tokens.Datos[0].Hash);
    }

    [Fact]
    public async Task Login_EmailInexistente_MismoErrorQueClaveIncorrecta()
    {
        var inexistente = await Assert.ThrowsAsync<NoAutenticadoException>(() => Login(email: "nadie@empresa.cl"));
        var incorrecta = await Assert.ThrowsAsync<NoAutenticadoException>(() => Login(clave: "otra"));

        Assert.Equal(inexistente.Message, incorrecta.Message);
    }

    [Fact]
    public async Task Login_CincoFallos_BloqueaAunqueLuegoLaClaveSeaCorrecta()
    {
        for (var i = 0; i < Usuario.IntentosAntesDeBloqueo; i++)
        {
            await Assert.ThrowsAsync<NoAutenticadoException>(() => Login(clave: "incorrecta"));
        }

        await Assert.ThrowsAsync<NoAutenticadoException>(() => Login());

        _reloj.Avanzar(TimeSpan.FromMinutes(16));
        Assert.NotNull(await Login());
    }

    [Fact]
    public async Task Login_EmpleadoDesvinculado_Rechazado()
    {
        _empleados.Datos[10].Desvincular(new DateOnly(2026, 9, 1));

        await Assert.ThrowsAsync<NoAutenticadoException>(() => Login());
    }

    [Fact]
    public async Task Renovar_RotaElToken()
    {
        var primero = await Login();

        var segundo = await Servicio().RenovarAsync(new RenovarTokenComando(primero.TokenRenovacion), CancellationToken.None);

        Assert.NotEqual(primero.TokenRenovacion, segundo.TokenRenovacion);
        Assert.True(_tokens.Datos[0].EstaRevocado);
        Assert.False(_tokens.Datos[1].EstaRevocado);
    }

    [Fact]
    public async Task Renovar_TokenYaUsado_RevocaTodasLasSesiones()
    {
        var primero = await Login();
        var segundo = await Servicio().RenovarAsync(new RenovarTokenComando(primero.TokenRenovacion), CancellationToken.None);

        // Un atacante reutiliza el token viejo:
        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            Servicio().RenovarAsync(new RenovarTokenComando(primero.TokenRenovacion), CancellationToken.None));

        // ...y el token legítimo también queda invalidado.
        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            Servicio().RenovarAsync(new RenovarTokenComando(segundo.TokenRenovacion), CancellationToken.None));
    }

    [Fact]
    public async Task Renovar_DespuesDeCambioDeSello_Rechazado()
    {
        var tokens = await Login();
        _usuarios.Datos[0].CambiarClave(Hasher.Prefijo + "Otra.Clave2026");

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            Servicio().RenovarAsync(new RenovarTokenComando(tokens.TokenRenovacion), CancellationToken.None));
    }

    [Fact]
    public async Task Renovar_Expirado_Rechazado()
    {
        var tokens = await Login();
        _reloj.Avanzar(TimeSpan.FromDays(8));

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            Servicio().RenovarAsync(new RenovarTokenComando(tokens.TokenRenovacion), CancellationToken.None));
    }

    // ---------- Dobles de prueba ----------

    private static void EstablecerId(object entidad, int id) =>
        entidad.GetType().GetProperty("Id")!.GetSetMethod(nonPublic: true)!.Invoke(entidad, [id]);

    private sealed class Reloj(DateTimeOffset inicio) : TimeProvider
    {
        private DateTimeOffset _ahora = inicio;
        public void Avanzar(TimeSpan t) => _ahora += t;
        public override DateTimeOffset GetUtcNow() => _ahora;
    }

    private sealed class Hasher : IHasherClaves
    {
        public const string Prefijo = "hash:";
        public string Hashear(string clave) => Prefijo + clave;
        public bool Verificar(string hash, string clave) => hash == Prefijo + clave;
    }

    private sealed class Generador : IGeneradorTokens
    {
        private int _n;
        public TimeSpan VigenciaRenovacion => TimeSpan.FromDays(7);
        public TokenAcceso GenerarAcceso(Usuario usuario, DateTimeOffset ahora) => new($"jwt-{usuario.Id}-{++_n}", ahora.AddMinutes(15));
        public string GenerarTokenRenovacion() => Guid.NewGuid().ToString("N");
        public string CalcularHash(string token) => "sha:" + token;
    }

    private sealed class Usuarios : IUsuarioRepositorio
    {
        public List<Usuario> Datos { get; } = [];
        public Task<Usuario?> ObtenerAsync(int id, CancellationToken ct) => Task.FromResult(Datos.FirstOrDefault(u => u.Id == id));
        public Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken ct) => Task.FromResult(Datos.FirstOrDefault(u => u.Email == email));
        public Task<bool> ExisteEmailAsync(string email, CancellationToken ct) => Task.FromResult(Datos.Any(u => u.Email == email));
        public Task<bool> ExisteParaEmpleadoAsync(int empleadoId, CancellationToken ct) => Task.FromResult(Datos.Any(u => u.EmpleadoId == empleadoId));
        public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => Task.FromResult(Datos.Count > 0);
        public void Agregar(Usuario usuario) => Datos.Add(usuario);
    }

    private sealed class Tokens : ITokenRenovacionRepositorio
    {
        public List<TokenRenovacion> Datos { get; } = [];
        public Task<TokenRenovacion?> ObtenerPorHashAsync(string hash, CancellationToken ct) => Task.FromResult(Datos.FirstOrDefault(t => t.Hash == hash));

        public Task RevocarTodosAsync(int usuarioId, DateTimeOffset ahora, string motivo, CancellationToken ct)
        {
            Datos.Where(t => t.UsuarioId == usuarioId).ToList().ForEach(t => t.Revocar(ahora, motivo));
            return Task.CompletedTask;
        }

        public void Agregar(TokenRenovacion token) => Datos.Add(token);
    }

    private sealed class Empleados : IEmpleadoRepositorio
    {
        public Dictionary<int, Empleado> Datos { get; } = [];
        public Task<Empleado?> ObtenerAsync(int id, CancellationToken ct) => Task.FromResult(Datos.GetValueOrDefault(id));
        public Task<bool> ExisteRutAsync(Rut rut, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct) => Task.FromResult(false);
        public Task<int?> ObtenerJefeIdAsync(int empleadoId, CancellationToken ct) => Task.FromResult<int?>(null);
        public void Agregar(Empleado empleado) { }
    }

    private sealed class AuditoriaNula : IRegistroAuditoria
    {
        public Task RegistrarAsync(string accion, string? detalle, int? codigo, int? usuarioId = null, string? email = null, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class UnidadNula : IUnidadDeTrabajo
    {
        public Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class SinUsuario : IUsuarioActual
    {
        public int? UsuarioId => null;
        public int? EmpleadoId => null;
        public string? Email => null;
        public Rol? Rol => null;
        public IReadOnlyList<int> Regiones => [];
        public string? Ip => null;
    }
}
