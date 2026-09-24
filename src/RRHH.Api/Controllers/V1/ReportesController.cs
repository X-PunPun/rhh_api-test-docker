using Microsoft.AspNetCore.Mvc;
using RRHH.Application.Empleados;
using RRHH.Application.Reportes;

namespace RRHH.Api.Controllers.V1;

/// <summary>Indicadores y exportaciones.</summary>
[ApiController]
[Route("api/v1/reportes")]
public sealed class ReportesController(IReportesServicio reportes) : ControllerBase
{
    /// <summary>Resumen para el dashboard: dotación por región y departamento, vacaciones pendientes, seguros vigentes.</summary>
    [HttpGet("resumen")]
    [Produces("application/json")]
    public Task<ResumenDto> Resumen(CancellationToken ct) => reportes.ObtenerResumenAsync(ct);

    /// <summary>Exporta empleados a Excel (.xlsx) con los mismos filtros de la búsqueda (máx. 5.000 filas).</summary>
    [HttpGet("empleados/excel")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, TiposContenido.Xlsx)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExportarEmpleados([FromQuery] FiltroEmpleados filtro, CancellationToken ct)
    {
        var archivo = await reportes.ExportarEmpleadosAsync(filtro, ct);
        return File(archivo.Contenido, archivo.TipoContenido, archivo.NombreArchivo);
    }
}
