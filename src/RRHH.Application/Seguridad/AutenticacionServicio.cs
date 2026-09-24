using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Domain.Seguridad;

namespace RRHH.Application.Seguridad;

public interface IAutenticacionServicio
{
    Task<TokensDto> IniciarSesionAsync(LoginComando comando, CancellationToken ct);
    Task<TokensDto> RenovarAsync(RenovarTokenComando comando, CancellationToken ct);
    Task CerrarSesionAsync(RenovarTokenComando comando, CancellationToken ct);
    Task<PerfilDto> ObtenerPerfilAsync(CancellationToken ct);
    Task CambiarClaveAsync(CambiarClaveComando comando, CancellationToken ct);
}

internal sealed class AutenticacionServicio(
    IUsuarioRepositorio usuarios,
    ITokenRenovacionRepositorio tokens,
    IEmpleadoRepositorio empleados,
    IGeneradorTokens generador,
    IHasherClaves hasher,
    IRegistroAuditoria auditoria,
    IUsuarioActual usuarioActual,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj) : IAutenticacionServicio
{
    /// <summary>Mismo mensaje para todo fallo de login: no revela si el email existe ni si la cuenta está bloqueada.</summary>
    private const string MensajeLoginFallido = "Credenciales inválidas o cuenta bloqueada temporalmente.";
    private const string MensajeSesionInvalida = "La sesión expiró o no es válida. Inicie sesión nuevamente.";

    private static string? _hashFicticio;

    public async Task<TokensDto> IniciarSesionAsync(LoginComando comando, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var email = Usuario.NormalizarEmail(comando.Email);
        var usuario = await usuarios.ObtenerPorEmailAsync(email, ct);

        if (usuario is null)
        {
            // Se verifica igual contra un hash ficticio para que el tiempo de respuesta
            // no delate si el email existe (enumeración de usuarios).
            hasher.Verificar(_hashFicticio ??= hasher.Hashear(Guid.NewGuid().ToString()), comando.Clave);
            await auditoria.RegistrarAsync("login.fallido", "usuario inexistente", 401, email: email, ct: ct);
            throw new NoAutenticadoException(MensajeLoginFallido);
        }

        if (!usuario.Activo || usuario.EstaBloqueado(ahora) || !await EmpleadoActivoAsync(usuario, ct))
        {
            await auditoria.RegistrarAsync("login.rechazado", "cuenta inactiva o bloqueada", 401, usuario.Id, usuario.Email, ct);
            throw new NoAutenticadoException(MensajeLoginFallido);
        }

        if (!hasher.Verificar(usuario.HashClave, comando.Clave))
        {
            usuario.RegistrarAccesoFallido(ahora);
            await unidadDeTrabajo.GuardarCambiosAsync(ct);

            var detalle = usuario.EstaBloqueado(ahora) ? "clave incorrecta; cuenta bloqueada" : "clave incorrecta";
            await auditoria.RegistrarAsync("login.fallido", detalle, 401, usuario.Id, usuario.Email, ct);
            throw new NoAutenticadoException(MensajeLoginFallido);
        }

        usuario.RegistrarAccesoExitoso(ahora);
        var resultado = await EmitirTokensAsync(usuario, ahora, ct);
        await auditoria.RegistrarAsync("login.exitoso", null, 200, usuario.Id, usuario.Email, ct);

        return resultado;
    }

    public async Task<TokensDto> RenovarAsync(RenovarTokenComando comando, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var token = await tokens.ObtenerPorHashAsync(generador.CalcularHash(comando.TokenRenovacion), ct)
            ?? throw new NoAutenticadoException(MensajeSesionInvalida);

        if (token.EstaRevocado)
        {
            // Un token ya usado vuelve a aparecer: alguien lo copió. Se cortan todas las sesiones del usuario.
            await tokens.RevocarTodosAsync(token.UsuarioId, ahora, "reutilización detectada", ct);
            await unidadDeTrabajo.GuardarCambiosAsync(ct);
            await auditoria.RegistrarAsync("token.reutilizado", "se revocaron todas las sesiones", 401, token.UsuarioId, ct: ct);
            throw new NoAutenticadoException(MensajeSesionInvalida);
        }

        if (!token.EstaVigente(ahora))
        {
            throw new NoAutenticadoException(MensajeSesionInvalida);
        }

        var usuario = await usuarios.ObtenerAsync(token.UsuarioId, ct);
        if (usuario is null || !usuario.Activo || usuario.EstaBloqueado(ahora) ||
            usuario.SelloSeguridad != token.SelloSeguridad || !await EmpleadoActivoAsync(usuario, ct))
        {
            token.Revocar(ahora, "usuario modificado o inactivo");
            await unidadDeTrabajo.GuardarCambiosAsync(ct);
            throw new NoAutenticadoException(MensajeSesionInvalida);
        }

        token.Revocar(ahora, "rotado");
        return await EmitirTokensAsync(usuario, ahora, ct);
    }

    public async Task CerrarSesionAsync(RenovarTokenComando comando, CancellationToken ct)
    {
        var token = await tokens.ObtenerPorHashAsync(generador.CalcularHash(comando.TokenRenovacion), ct);
        if (token is null || token.EstaRevocado)
        {
            return; // Idempotente: no revela si el token existía.
        }

        token.Revocar(reloj.GetUtcNow(), "logout");
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        await auditoria.RegistrarAsync("logout", null, 204, token.UsuarioId, ct: ct);
    }

    public async Task<PerfilDto> ObtenerPerfilAsync(CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerAsync(usuarioActual.UsuarioIdRequerido(), ct)
            ?? throw new NoAutenticadoException(MensajeSesionInvalida);

        return await CrearPerfilAsync(usuario, ct);
    }

    public async Task CambiarClaveAsync(CambiarClaveComando comando, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var usuario = await usuarios.ObtenerAsync(usuarioActual.UsuarioIdRequerido(), ct)
            ?? throw new NoAutenticadoException(MensajeSesionInvalida);

        if (!hasher.Verificar(usuario.HashClave, comando.ClaveActual))
        {
            await auditoria.RegistrarAsync("clave.cambio_fallido", "clave actual incorrecta", 403, usuario.Id, usuario.Email, ct);
            throw new AccesoDenegadoException("La clave actual no es correcta.");
        }

        PoliticaClaves.Validar(comando.ClaveNueva, usuario.Email);

        usuario.CambiarClave(hasher.Hashear(comando.ClaveNueva));
        await tokens.RevocarTodosAsync(usuario.Id, ahora, "cambio de clave", ct);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        await auditoria.RegistrarAsync("clave.cambiada", null, 204, usuario.Id, usuario.Email, ct);
    }

    private async Task<TokensDto> EmitirTokensAsync(Usuario usuario, DateTimeOffset ahora, CancellationToken ct)
    {
        var acceso = generador.GenerarAcceso(usuario, ahora);
        var valorRenovacion = generador.GenerarTokenRenovacion();
        var renovacion = TokenRenovacion.Crear(
            usuario.Id, generador.CalcularHash(valorRenovacion), usuario.SelloSeguridad, ahora, generador.VigenciaRenovacion);

        tokens.Agregar(renovacion);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return new TokensDto(acceso.Token, acceso.ExpiraEn, valorRenovacion, renovacion.ExpiraEn, await CrearPerfilAsync(usuario, ct));
    }

    private async Task<PerfilDto> CrearPerfilAsync(Usuario usuario, CancellationToken ct)
    {
        string? nombre = null;
        if (usuario.EmpleadoId is { } empleadoId && await empleados.ObtenerAsync(empleadoId, ct) is { } empleado)
        {
            nombre = empleado.NombreCompleto;
        }

        return new PerfilDto(usuario.Id, usuario.Email, usuario.Rol, usuario.EmpleadoId, nombre, usuario.Regiones);
    }

    /// <summary>Un empleado desvinculado pierde el acceso aunque su usuario siga activo.</summary>
    private async Task<bool> EmpleadoActivoAsync(Usuario usuario, CancellationToken ct) =>
        usuario.EmpleadoId is not { } empleadoId || (await empleados.ObtenerAsync(empleadoId, ct))?.Activo == true;
}
