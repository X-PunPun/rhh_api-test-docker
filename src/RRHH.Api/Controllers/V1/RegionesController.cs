using Microsoft.AspNetCore.Mvc;
using RRHH.Application.Ubicacion;

namespace RRHH.Api.Controllers.V1;

/// <summary>Catálogo de regiones y comunas de Chile.</summary>
[ApiController]
[Route("api/v1/regiones")]
[Produces("application/json")]
public sealed class RegionesController(IUbicacionServicio ubicacion) : ControllerBase
{
    /// <summary>Lista las 16 regiones ordenadas de norte a sur.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RegionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RegionDto>>> Listar(CancellationToken ct)
    {
        return Ok(await ubicacion.ListarRegionesAsync(ct));
    }

    /// <summary>Lista las comunas de una región.</summary>
    /// <param name="regionId">Código CUT de la región (1 a 16).</param>
    [HttpGet("{regionId:int}/comunas")]
    [ProducesResponseType<IReadOnlyList<ComunaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ComunaDto>>> ListarComunas(int regionId, CancellationToken ct)
    {
        var comunas = await ubicacion.ListarComunasAsync(regionId, ct);
        return comunas is null ? NotFound() : Ok(comunas);
    }
}
