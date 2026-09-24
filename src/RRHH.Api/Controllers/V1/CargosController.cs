using Microsoft.AspNetCore.Mvc;
using RRHH.Application.Organizacion;

namespace RRHH.Api.Controllers.V1;

/// <summary>Cargos (puestos de trabajo) por departamento.</summary>
[ApiController]
[Route("api/v1/cargos")]
[Produces("application/json")]
public sealed class CargosController(IOrganizacionServicio organizacion) : ControllerBase
{
    /// <summary>Lista cargos, opcionalmente filtrados por departamento y estado.</summary>
    [HttpGet]
    public Task<IReadOnlyList<CargoDto>> Listar([FromQuery] int? departamentoId, [FromQuery] bool? activo, CancellationToken ct) =>
        organizacion.ListarCargosAsync(departamentoId, activo, ct);

    /// <summary>Obtiene un cargo.</summary>
    [HttpGet("{id:int}", Name = "ObtenerCargo")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CargoDto> Obtener(int id, CancellationToken ct) => organizacion.ObtenerCargoAsync(id, ct);

    /// <summary>Crea un cargo dentro de un departamento activo.</summary>
    [HttpPost]
    [ProducesResponseType<CargoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CargoDto>> Crear(CrearCargoComando comando, CancellationToken ct)
    {
        var dto = await organizacion.CrearCargoAsync(comando, ct);
        return CreatedAtRoute("ObtenerCargo", new { id = dto.Id }, dto);
    }

    /// <summary>Renombra un cargo.</summary>
    [HttpPut("{id:int}")]
    public Task<CargoDto> Renombrar(int id, RenombrarCargoComando comando, CancellationToken ct) =>
        organizacion.RenombrarCargoAsync(id, comando, ct);

    /// <summary>Desactiva un cargo.</summary>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await organizacion.DesactivarCargoAsync(id, ct);
        return NoContent();
    }
}
