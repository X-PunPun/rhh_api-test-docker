using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Domain.Seguridad;

namespace RRHH.Application.Seguridad;

public interface IUsuariosServicio
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct);
    Task<UsuarioDto> ObtenerAsync(int id, CancellationToken ct);
    Task<UsuarioDto> CrearAsync(CrearUsuarioComando comando, CancellationToken ct);
    Task<UsuarioDto> AsignarRolAsync(int id, AsignarRolComando comando, CancellationToken ct);
    Task CambiarEstadoAsync(int id, bool activo, CancellationToken ct);
    Task DesbloquearAsync(int id, CancellationToken ct);
    Task RestablecerClaveAsync(int id, RestablecerClaveComando comando, CancellationToken ct);
    Task<Pagina<RegistroAuditoriaDto>> ListarAuditoriaAsync(FiltroAuditoria filtro, CancellationToken ct);
}

/// <summary>Administración de cuentas. Todas las operaciones exigen rol Admin (además de la política del controlador).</summary>
internal sealed class UsuariosServicio(
    IUsuarioRepositorio usuarios,
    IUsuarioConsultas consultas,
    ITokenRenovacionRepositorio tokens,
    IEmpleadoRepositorio empleados,
    IHasherClaves hasher,
    IRegistroAuditoria auditoria,
    IUsuarioActual usuarioActual,
    IUnidadDeTrabajo unidadDeTrabajo,
    TimeProvider reloj) : IUsuariosServicio
{
    public Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct)
    {
        usuarioActual.ExigirAdmin();
        return consultas.ListarAsync(reloj.GetUtcNow(), ct);
    }

    public async Task<UsuarioDto> ObtenerAsync(int id, CancellationToken ct)
    {
        usuarioActual.ExigirAdmin();
        return await consultas.ObtenerAsync(id, reloj.GetUtcNow(), ct) ?? throw new RecursoNoEncontradoException("Usuario", id);
    }

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioComando comando, CancellationToken ct)
    {
        usuarioActual.ExigirAdmin();

        string email;
        if (comando.EmpleadoId is { } empleadoId)
        {
            var empleado = await empleados.ObtenerAsync(empleadoId, ct)
                ?? throw new RecursoNoEncontradoException("Empleado", empleadoId);

            if (!empleado.Activo)
            {
                throw new ConflictoException("No se puede crear un usuario para un empleado desvinculado.");
            }

            if (await usuarios.ExisteParaEmpleadoAsync(empleadoId, ct))
            {
                throw new ConflictoException("El empleado ya tiene un usuario.");
            }

            email = empleado.Email;
        }
        else
        {
            email = Usuario.NormalizarEmail(comando.Email
                ?? throw new Domain.Comun.ExcepcionDominio("Indique un empleado o un email."));
        }

        if (await usuarios.ExisteEmailAsync(email, ct))
        {
            throw new ConflictoException($"Ya existe un usuario con el email '{email}'.");
        }

        PoliticaClaves.Validar(comando.ClaveInicial, email);

        var usuario = Usuario.Crear(email, hasher.Hashear(comando.ClaveInicial), comando.Rol, comando.EmpleadoId,
            comando.Regiones, reloj.GetUtcNow());

        usuarios.Agregar(usuario);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        await auditoria.RegistrarAsync("usuario.creado", $"{usuario.Email} ({usuario.Rol})", 201, ct: ct);

        return await ObtenerAsync(usuario.Id, ct);
    }

    public async Task<UsuarioDto> AsignarRolAsync(int id, AsignarRolComando comando, CancellationToken ct)
    {
        usuarioActual.ExigirAdmin();
        var usuario = await ObtenerEntidadDistintaDeMiAsync(id, "cambiar su propio rol", ct);

        usuario.AsignarRol(comando.Rol, comando.Regiones);
        await tokens.RevocarTodosAsync(id, reloj.GetUtcNow(), "cambio de rol", ct);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        await auditoria.RegistrarAsync("usuario.rol", $"{usuario.Email} → {usuario.Rol} [{string.Join(",", usuario.Regiones)}]", 200, ct: ct);

        return await ObtenerAsync(id, ct);
    }

    public async Task CambiarEstadoAsync(int id, bool activo, CancellationToken ct)
    {
        usuarioActual.ExigirAdmin();
        var usuario = await ObtenerEntidadDistintaDeMiAsync(id, "desactivarse a sí mismo", ct);

        if (activo)
        {
            usuario.Activar();
        }
        else
        {
            usuario.Desactivar();
            await tokens.RevocarTodosAsync(id, reloj.GetUtcNow(), "usuario desactivado", ct);
        }

        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        await auditoria.RegistrarAsync(activo ? "usuario.activado" : "usuario.desactivado", usuario.Email, 204, ct: ct);
    }

    public async Task DesbloquearAsync(int id, CancellationToken ct)
    {
        usuarioActual.ExigirAdmin();
        var usuario = await usuarios.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Usuario", id);

        usuario.Desbloquear();
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        await auditoria.RegistrarAsync("usuario.desbloqueado", usuario.Email, 204, ct: ct);
    }

    public async Task RestablecerClaveAsync(int id, RestablecerClaveComando comando, CancellationToken ct)
    {
        usuarioActual.ExigirAdmin();
        var usuario = await usuarios.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Usuario", id);

        PoliticaClaves.Validar(comando.ClaveNueva, usuario.Email);
        usuario.CambiarClave(hasher.Hashear(comando.ClaveNueva));
        usuario.Desbloquear();
        await tokens.RevocarTodosAsync(id, reloj.GetUtcNow(), "clave restablecida por admin", ct);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
        await auditoria.RegistrarAsync("usuario.clave_restablecida", usuario.Email, 204, ct: ct);
    }

    public Task<Pagina<RegistroAuditoriaDto>> ListarAuditoriaAsync(FiltroAuditoria filtro, CancellationToken ct)
    {
        usuarioActual.ExigirAdmin();
        return consultas.ListarAuditoriaAsync(filtro, ct);
    }

    /// <summary>Evita que un admin se quite privilegios o se bloquee a sí mismo por error.</summary>
    private async Task<Usuario> ObtenerEntidadDistintaDeMiAsync(int id, string accion, CancellationToken ct)
    {
        if (id == usuarioActual.UsuarioId)
        {
            throw new ConflictoException($"Un administrador no puede {accion}.");
        }

        return await usuarios.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Usuario", id);
    }
}
