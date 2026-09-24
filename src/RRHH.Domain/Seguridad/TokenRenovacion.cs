using RRHH.Domain.Comun;

namespace RRHH.Domain.Seguridad;

/// <summary>
/// Token de renovación (refresh token). Se guarda solo su hash SHA-256.
/// Cada uso lo revoca y emite uno nuevo (rotación); si llega uno ya revocado,
/// se asume robo y se revocan todos los tokens del usuario.
/// </summary>
public sealed class TokenRenovacion : Entidad<int>
{
    public int UsuarioId { get; private set; }
    public string Hash { get; private set; } = null!;
    public string SelloSeguridad { get; private set; } = null!;
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ExpiraEn { get; private set; }
    public DateTimeOffset? RevocadoEn { get; private set; }
    public string? MotivoRevocacion { get; private set; }

    private TokenRenovacion() { } // EF Core

    public static TokenRenovacion Crear(int usuarioId, string hash, string selloSeguridad, DateTimeOffset ahora, TimeSpan vigencia)
    {
        return new TokenRenovacion
        {
            UsuarioId = Guardia.IdPositivo(usuarioId, nameof(UsuarioId)),
            Hash = Guardia.TextoRequerido(hash, nameof(Hash), 100),
            SelloSeguridad = selloSeguridad,
            CreadoEn = ahora,
            ExpiraEn = ahora.Add(vigencia),
        };
    }

    public bool EstaRevocado => RevocadoEn is not null;

    public bool EstaVigente(DateTimeOffset ahora) => !EstaRevocado && ExpiraEn > ahora;

    public void Revocar(DateTimeOffset ahora, string motivo)
    {
        if (EstaRevocado)
        {
            return;
        }

        RevocadoEn = ahora;
        MotivoRevocacion = motivo;
    }
}
