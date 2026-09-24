namespace RRHH.Application.Reportes;

public sealed record ConteoDto(int Id, string Nombre, int Cantidad);

public sealed record ResumenDto(
    DateOnly Fecha,
    int EmpleadosActivos,
    int EmpleadosDesvinculados,
    int SolicitudesVacacionesPendientes,
    int AfiliacionesSeguroVigentes,
    IReadOnlyList<ConteoDto> EmpleadosPorRegion,
    IReadOnlyList<ConteoDto> EmpleadosPorDepartamento);

/// <summary>Archivo generado listo para descargar.</summary>
public sealed record ArchivoDto(byte[] Contenido, string NombreArchivo, string TipoContenido);

/// <summary>Definición de una columna para el exportador de planillas.</summary>
public sealed record ColumnaExcel<T>(string Titulo, Func<T, object?> Valor);

public static class TiposContenido
{
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
