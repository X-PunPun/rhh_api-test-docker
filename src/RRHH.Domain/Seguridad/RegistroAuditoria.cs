namespace RRHH.Domain.Seguridad;

/// <summary>Registro inmutable de una acción relevante: quién, qué, cuándo y con qué resultado.</summary>
public sealed class RegistroAuditoria
{
    public long Id { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public int? UsuarioId { get; private set; }
    public string? Email { get; private set; }
    public string Accion { get; private set; } = null!;
    public string? Detalle { get; private set; }
    public int? CodigoResultado { get; private set; }
    public string? Ip { get; private set; }

    private RegistroAuditoria() { } // EF Core

    public static RegistroAuditoria Crear(
        DateTimeOffset fecha, int? usuarioId, string? email, string accion, string? detalle, int? codigoResultado, string? ip) =>
        new()
        {
            Fecha = fecha,
            UsuarioId = usuarioId,
            Email = Recortar(email, 150),
            Accion = Recortar(accion, 200) ?? "desconocida",
            Detalle = Recortar(detalle, 500),
            CodigoResultado = codigoResultado,
            Ip = Recortar(ip, 64),
        };

    private static string? Recortar(string? texto, int largo) =>
        texto is null ? null : texto.Length <= largo ? texto : texto[..largo];
}
