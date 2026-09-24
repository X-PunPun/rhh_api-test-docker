using Microsoft.AspNetCore.Mvc;
using RRHH.Application.Vacaciones;

namespace RRHH.Api.Controllers.V1;

/// <summary>Calendario de feriados (no se descuentan de las vacaciones).</summary>
[ApiController]
[Route("api/v1/feriados")]
[Produces("application/json")]
public sealed class FeriadosController(IFeriadoServicio feriados, TimeProvider reloj) : ControllerBase
{
    /// <summary>Feriados de un año (por defecto, el actual).</summary>
    [HttpGet]
    public Task<IReadOnlyList<FeriadoDto>> Listar([FromQuery] int? anio, CancellationToken ct) =>
        feriados.ListarAsync(anio ?? reloj.GetLocalNow().Year, ct);

    /// <summary>Agrega un feriado.</summary>
    [HttpPost]
    [ProducesResponseType<FeriadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FeriadoDto>> Crear(CrearFeriadoComando comando, CancellationToken ct)
    {
        var dto = await feriados.CrearAsync(comando, ct);
        return Created($"/api/v1/feriados?anio={dto.Fecha.Year}", dto);
    }

    /// <summary>Elimina un feriado.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await feriados.EliminarAsync(id, ct);
        return NoContent();
    }
}
