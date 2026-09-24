using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Application.Seguridad;

namespace RRHH.Application.Reportes;

public interface IReportesServicio
{
    Task<ResumenDto> ObtenerResumenAsync(CancellationToken ct);
    Task<ArchivoDto> ExportarEmpleadosAsync(FiltroEmpleados filtro, CancellationToken ct);
}

internal sealed class ReportesServicio(
    IReportesConsultas consultas,
    IEmpleadoConsultas empleados,
    IExportadorExcel exportador,
    IUsuarioActual usuarioActual,
    IRegistroAuditoria auditoria,
    TimeProvider reloj) : IReportesServicio
{
    /// <summary>Tope de filas por exportación (protege memoria y evita extracciones masivas).</summary>
    public const int MaximoFilasExportacion = 5000;

    private static readonly IReadOnlyList<ColumnaExcel<EmpleadoResumenDto>> ColumnasEmpleados =
    [
        new("RUT", e => e.Rut),
        new("Nombre completo", e => e.NombreCompleto),
        new("Email", e => e.Email),
        new("Cargo", e => e.Cargo),
        new("Departamento", e => e.Departamento),
        new("Región", e => e.Region),
        new("Comuna", e => e.Comuna),
        new("Jefatura", e => e.Jefe),
        new("Fecha ingreso", e => e.FechaIngreso),
        new("Estado", e => e.Activo ? "Activo" : "Desvinculado"),
    ];

    public Task<ResumenDto> ObtenerResumenAsync(CancellationToken ct)
    {
        usuarioActual.ExigirGestor();
        return consultas.ObtenerResumenAsync(reloj.Hoy(), usuarioActual.Alcance(), ct);
    }

    public async Task<ArchivoDto> ExportarEmpleadosAsync(FiltroEmpleados filtro, CancellationToken ct)
    {
        usuarioActual.ExigirGestor();

        // La exportación aplica el mismo alcance que la búsqueda: no es un atajo para ver más datos.
        var filas = await empleados.ListarParaExportarAsync(filtro, usuarioActual.Alcance(), MaximoFilasExportacion, ct);

        if (filas.Count > MaximoFilasExportacion)
        {
            throw new ConflictoException(
                $"La exportación supera el máximo de {MaximoFilasExportacion} filas. Aplique más filtros.");
        }

        var contenido = exportador.Generar("Empleados", filas, ColumnasEmpleados);
        var nombre = $"empleados_{reloj.GetLocalNow():yyyyMMdd_HHmm}.xlsx";

        await auditoria.RegistrarAsync("reporte.empleados_excel", $"{filas.Count} filas", 200, ct: ct);

        return new ArchivoDto(contenido, nombre, TiposContenido.Xlsx);
    }
}
