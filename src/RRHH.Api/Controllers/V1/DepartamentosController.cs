using Microsoft.AspNetCore.Mvc;
using RRHH.Application.Organizacion;

namespace RRHH.Api.Controllers.V1;

/// <summary>Departamentos de la organización.</summary>
[ApiController]
[Route("api/v1/departamentos")]
[Produces("application/json")]
public sealed class DepartamentosController(IOrganizacionServicio organizacion) : ControllerBase
{
    /// <summary>Lista los departamentos con su dotación activa.</summary>
    /// <param name="activo">Filtra por estado (opcional).</param>
    [HttpGet]
    public Task<IReadOnlyList<DepartamentoDto>> Listar([FromQuery] bool? activo, CancellationToken ct) =>
        organizacion.ListarDepartamentosAsync(activo, ct);

    /// <summary>Obtiene un departamento.</summary>
    [HttpGet("{id:int}", Name = "ObtenerDepartamento")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DepartamentoDto> Obtener(int id, CancellationToken ct) => organizacion.ObtenerDepartamentoAsync(id, ct);

    /// <summary>Crea un departamento.</summary>
    [HttpPost]
    [ProducesResponseType<DepartamentoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartamentoDto>> Crear(GuardarDepartamentoComando comando, CancellationToken ct)
    {
        var dto = await organizacion.CrearDepartamentoAsync(comando, ct);
        return CreatedAtRoute("ObtenerDepartamento", new { id = dto.Id }, dto);
    }

    /// <summary>Actualiza nombre y descripción.</summary>
    [HttpPut("{id:int}")]
    public Task<DepartamentoDto> Actualizar(int id, GuardarDepartamentoComando comando, CancellationToken ct) =>
        organizacion.ActualizarDepartamentoAsync(id, comando, ct);

    /// <summary>Activa un departamento.</summary>
    [HttpPost("{id:int}/activar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        await organizacion.CambiarEstadoDepartamentoAsync(id, activo: true, ct);
        return NoContent();
    }

    /// <summary>Desactiva un departamento (solo si no tiene empleados activos).</summary>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await organizacion.CambiarEstadoDepartamentoAsync(id, activo: false, ct);
        return NoContent();
    }
}
