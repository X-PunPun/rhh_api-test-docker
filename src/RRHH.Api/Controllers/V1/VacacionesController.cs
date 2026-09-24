using Microsoft.AspNetCore.Mvc;
using RRHH.Application.Vacaciones;

namespace RRHH.Api.Controllers.V1;

/// <summary>Flujo de aprobación de vacaciones.</summary>
[ApiController]
[Route("api/v1/vacaciones")]
[Produces("application/json")]
public sealed class VacacionesController(IVacacionesServicio vacaciones) : ControllerBase
{
    /// <summary>Obtiene una solicitud de vacaciones.</summary>
    [HttpGet("{id:int}", Name = "ObtenerSolicitudVacaciones")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SolicitudVacacionesDto> Obtener(int id, CancellationToken ct) => vacaciones.ObtenerAsync(id, ct);

    /// <summary>Solicitudes pendientes de los subordinados directos del usuario actual.</summary>
    [HttpGet("pendientes-equipo")]
    public Task<IReadOnlyList<SolicitudVacacionesDto>> PendientesDeMiEquipo(CancellationToken ct) =>
        vacaciones.ListarPendientesDeMiEquipoAsync(ct);

    /// <summary>Aprueba una solicitud (solo la jefatura directa).</summary>
    [HttpPost("{id:int}/aprobar")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<SolicitudVacacionesDto> Aprobar(int id, CancellationToken ct) => vacaciones.AprobarAsync(id, ct);

    /// <summary>Rechaza una solicitud indicando el motivo (solo la jefatura directa).</summary>
    [HttpPost("{id:int}/rechazar")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<SolicitudVacacionesDto> Rechazar(int id, RechazarSolicitudComando comando, CancellationToken ct) =>
        vacaciones.RechazarAsync(id, comando, ct);

    /// <summary>El propio empleado cancela su solicitud pendiente.</summary>
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<SolicitudVacacionesDto> Cancelar(int id, CancellationToken ct) => vacaciones.CancelarAsync(id, ct);
}
