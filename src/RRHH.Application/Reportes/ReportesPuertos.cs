using RRHH.Application.Seguridad;

namespace RRHH.Application.Reportes;

/// <summary>Puerto de salida para generar planillas Excel (.xlsx).</summary>
public interface IExportadorExcel
{
    byte[] Generar<T>(string nombreHoja, IReadOnlyList<T> filas, IReadOnlyList<ColumnaExcel<T>> columnas);
}

public interface IReportesConsultas
{
    Task<ResumenDto> ObtenerResumenAsync(DateOnly hoy, AlcanceDatos alcance, CancellationToken ct);
}
