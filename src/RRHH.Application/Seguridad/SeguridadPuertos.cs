using RRHH.Application.Comun;
using RRHH.Domain.Seguridad;

namespace RRHH.Application.Seguridad;

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerAsync(int id, CancellationToken ct);

    Task<Usuario?> ObtenerPorEmailAsync(string emailNormalizado, CancellationToken ct);

    Task<bool> ExisteEmailAsync(string emailNormalizado, CancellationToken ct);

    Task<bool> ExisteParaEmpleadoAsync(int empleadoId, CancellationToken ct);

    Task<bool> ExisteAlgunoAsync(CancellationToken ct);

    void Agregar(Usuario usuario);
}

public interface ITokenRenovacionRepositorio
{
    Task<TokenRenovacion?> ObtenerPorHashAsync(string hash, CancellationToken ct);

    /// <summary>Revoca todos los tokens vigentes del usuario (cambio de clave, robo detectado, desactivación).</summary>
    Task RevocarTodosAsync(int usuarioId, DateTimeOffset ahora, string motivo, CancellationToken ct);

    void Agregar(TokenRenovacion token);
}

public sealed record TokenAcceso(string Token, DateTimeOffset ExpiraEn);

/// <summary>Puerto para emitir tokens (JWT de acceso y tokens de renovación aleatorios).</summary>
public interface IGeneradorTokens
{
    TimeSpan VigenciaRenovacion { get; }

    TokenAcceso GenerarAcceso(Usuario usuario, DateTimeOffset ahora);

    /// <summary>Valor aleatorio criptográficamente seguro (se entrega al cliente una sola vez).</summary>
    string GenerarTokenRenovacion();

    /// <summary>Hash SHA-256 del token de renovación (es lo único que se guarda).</summary>
    string CalcularHash(string token);
}

/// <summary>Puerto para el hash de claves (PBKDF2 en la implementación).</summary>
public interface IHasherClaves
{
    string Hashear(string clave);

    bool Verificar(string hash, string clave);
}

/// <summary>Puerto para registrar acciones en la bitácora de auditoría.</summary>
public interface IRegistroAuditoria
{
    Task RegistrarAsync(string accion, string? detalle, int? codigoResultado, int? usuarioId = null, string? email = null, CancellationToken ct = default);
}

public interface IUsuarioConsultas
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(DateTimeOffset ahora, CancellationToken ct);

    Task<UsuarioDto?> ObtenerAsync(int id, DateTimeOffset ahora, CancellationToken ct);

    Task<Pagina<RegistroAuditoriaDto>> ListarAuditoriaAsync(FiltroAuditoria filtro, CancellationToken ct);
}
