using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RRHH.Application.Comun;
using RRHH.Application.Seguridad;
using RRHH.Domain.Seguridad;
using RRHH.Infrastructure.Persistencia;

namespace RRHH.Infrastructure.Seguridad;

/// <summary>
/// Escribe la bitácora con un DbContext propio (otro scope): así un error en la operación
/// principal no impide registrar el intento, ni la auditoría arrastra cambios a medio guardar.
/// </summary>
internal sealed class RegistroAuditoriaEf(
    IServiceScopeFactory scopeFactory,
    IUsuarioActual usuarioActual,
    TimeProvider reloj,
    ILogger<RegistroAuditoriaEf> logger) : IRegistroAuditoria
{
    public async Task RegistrarAsync(
        string accion, string? detalle, int? codigoResultado, int? usuarioId = null, string? email = null, CancellationToken ct = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<RrhhDbContext>();

            db.Auditoria.Add(RegistroAuditoria.Crear(
                reloj.GetUtcNow(),
                usuarioId ?? usuarioActual.UsuarioId,
                email ?? usuarioActual.Email,
                accion,
                detalle,
                codigoResultado,
                usuarioActual.Ip));

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // La auditoría nunca debe tumbar la operación del usuario; se deja en el log.
            logger.LogError(ex, "No se pudo registrar la auditoría de {Accion}", accion);
        }
    }
}
