using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRHH.Api.Seguridad;
using RRHH.Application.Seguros;

namespace RRHH.Api.Controllers.V1;

/// <summary>Catálogo de planes de seguro complementario.</summary>
[ApiController]
[Route("api/v1/planes-seguro")]
[Produces("application/json")]
public sealed class PlanesSeguroController(ISegurosServicio seguros) : ControllerBase
{
    /// <summary>Lista los planes de seguro.</summary>
    [HttpGet]
    public Task<IReadOnlyList<PlanSeguroDto>> Listar([FromQuery] bool? activo, CancellationToken ct) =>
        seguros.ListarPlanesAsync(activo, ct);

    /// <summary>Obtiene un plan.</summary>
    [HttpGet("{id:int}", Name = "ObtenerPlanSeguro")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PlanSeguroDto> Obtener(int id, CancellationToken ct) => seguros.ObtenerPlanAsync(id, ct);

    /// <summary>Crea un plan.</summary>
    [Authorize(Policy = Politicas.Gestor)]
    [HttpPost]
    [ProducesResponseType<PlanSeguroDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanSeguroDto>> Crear(GuardarPlanSeguroComando comando, CancellationToken ct)
    {
        var dto = await seguros.CrearPlanAsync(comando, ct);
        return CreatedAtRoute("ObtenerPlanSeguro", new { id = dto.Id }, dto);
    }

    /// <summary>Actualiza un plan.</summary>
    [Authorize(Policy = Politicas.Gestor)]
    [HttpPut("{id:int}")]
    public Task<PlanSeguroDto> Actualizar(int id, GuardarPlanSeguroComando comando, CancellationToken ct) =>
        seguros.ActualizarPlanAsync(id, comando, ct);

    /// <summary>Activa un plan.</summary>
    [Authorize(Policy = Politicas.Gestor)]
    [HttpPost("{id:int}/activar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        await seguros.CambiarEstadoPlanAsync(id, activo: true, ct);
        return NoContent();
    }

    /// <summary>Desactiva un plan (no admite nuevas afiliaciones).</summary>
    [Authorize(Policy = Politicas.Gestor)]
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await seguros.CambiarEstadoPlanAsync(id, activo: false, ct);
        return NoContent();
    }
}
