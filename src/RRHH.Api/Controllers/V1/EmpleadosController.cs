using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRHH.Api.Seguridad;
using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Seguros;
using RRHH.Application.Vacaciones;
using RRHH.Domain.Vacaciones;

namespace RRHH.Api.Controllers.V1;

/// <summary>Personal interno: ficha, jerarquía, vacaciones y seguros.</summary>
[ApiController]
[Route("api/v1/empleados")]
[Produces("application/json")]
public sealed class EmpleadosController(
    IEmpleadoServicio empleados,
    IVacacionesServicio vacaciones,
    ISegurosServicio seguros) : ControllerBase
{
    /// <summary>Busca empleados con filtros (región, comuna, departamento, cargo, jefe, estado, texto) y paginación.</summary>
    [HttpGet]
    public Task<Pagina<EmpleadoResumenDto>> Buscar([FromQuery] FiltroEmpleados filtro, CancellationToken ct) =>
        empleados.BuscarAsync(filtro, ct);

    /// <summary>Ficha completa de un empleado.</summary>
    [HttpGet("{id:int}", Name = "ObtenerEmpleado")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmpleadoDetalleDto> Obtener(int id, CancellationToken ct) => empleados.ObtenerAsync(id, ct);

    /// <summary>Subordinados directos de un jefe.</summary>
    [HttpGet("{id:int}/subordinados")]
    public Task<IReadOnlyList<EmpleadoResumenDto>> Subordinados(int id, CancellationToken ct) =>
        empleados.ListarSubordinadosAsync(id, ct);

    /// <summary>Registra un empleado (valida RUT, email único, cargo/departamento y jefatura).</summary>
    [Authorize(Policy = Politicas.Gestor)]
    [HttpPost]
    [ProducesResponseType<EmpleadoDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmpleadoDetalleDto>> Crear(CrearEmpleadoComando comando, CancellationToken ct)
    {
        var dto = await empleados.CrearAsync(comando, ct);
        return CreatedAtRoute("ObtenerEmpleado", new { id = dto.Id }, dto);
    }

    /// <summary>Actualiza contacto, asignación, jefatura y previsión.</summary>
    [Authorize(Policy = Politicas.Gestor)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<EmpleadoDetalleDto> Actualizar(int id, ActualizarEmpleadoComando comando, CancellationToken ct) =>
        empleados.ActualizarAsync(id, comando, ct);

    /// <summary>Desvincula al empleado (no puede tener subordinados activos).</summary>
    [Authorize(Policy = Politicas.Gestor)]
    [HttpPost("{id:int}/desvincular")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desvincular(int id, DesvincularEmpleadoComando comando, CancellationToken ct)
    {
        await empleados.DesvincularAsync(id, comando, ct);
        return NoContent();
    }

    // ---------- Vacaciones ----------

    /// <summary>Saldo de feriado legal (devengado, tomado, pendiente y disponible).</summary>
    [HttpGet("{id:int}/vacaciones/saldo")]
    public Task<SaldoVacacionesDto> SaldoVacaciones(int id, CancellationToken ct) => vacaciones.ObtenerSaldoAsync(id, ct);

    /// <summary>Historial de solicitudes de vacaciones del empleado.</summary>
    [HttpGet("{id:int}/vacaciones")]
    public Task<IReadOnlyList<SolicitudVacacionesDto>> ListarVacaciones(int id, [FromQuery] EstadoSolicitud? estado, CancellationToken ct) =>
        vacaciones.ListarPorEmpleadoAsync(id, estado, ct);

    /// <summary>El propio empleado solicita vacaciones (se calculan días hábiles descontando feriados).</summary>
    [HttpPost("{id:int}/vacaciones")]
    [ProducesResponseType<SolicitudVacacionesDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SolicitudVacacionesDto>> SolicitarVacaciones(
        int id, SolicitarVacacionesComando comando, CancellationToken ct)
    {
        var dto = await vacaciones.SolicitarAsync(id, comando, ct);
        return CreatedAtRoute("ObtenerSolicitudVacaciones", new { id = dto.Id }, dto);
    }

    // ---------- Seguros ----------

    /// <summary>Seguros complementarios del empleado.</summary>
    [HttpGet("{id:int}/seguros")]
    public Task<IReadOnlyList<AfiliacionSeguroDto>> ListarSeguros(int id, CancellationToken ct) =>
        seguros.ListarAfiliacionesAsync(id, ct);

    /// <summary>Afilia al empleado a un plan de seguro.</summary>
    [Authorize(Policy = Politicas.Gestor)]
    [HttpPost("{id:int}/seguros")]
    [ProducesResponseType<AfiliacionSeguroDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AfiliacionSeguroDto>> Afiliar(int id, AfiliarSeguroComando comando, CancellationToken ct)
    {
        var dto = await seguros.AfiliarAsync(id, comando, ct);
        return Created($"/api/v1/empleados/{id}/seguros", dto);
    }

    /// <summary>Termina una afiliación de seguro del empleado.</summary>
    [Authorize(Policy = Politicas.Gestor)]
    [HttpPost("{id:int}/seguros/{afiliacionId:int}/terminar")]
    public Task<AfiliacionSeguroDto> TerminarSeguro(int id, int afiliacionId, TerminarAfiliacionComando comando, CancellationToken ct) =>
        seguros.TerminarAfiliacionAsync(id, afiliacionId, comando, ct);
}
