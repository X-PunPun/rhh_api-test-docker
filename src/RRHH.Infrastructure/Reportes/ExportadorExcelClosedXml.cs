using ClosedXML.Excel;
using RRHH.Application.Reportes;

namespace RRHH.Infrastructure.Reportes;

/// <summary>Adaptador de salida: genera .xlsx con ClosedXML (licencia MIT).</summary>
internal sealed class ExportadorExcelClosedXml : IExportadorExcel
{
    public byte[] Generar<T>(string nombreHoja, IReadOnlyList<T> filas, IReadOnlyList<ColumnaExcel<T>> columnas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(nombreHoja);

        for (var c = 0; c < columnas.Count; c++)
        {
            hoja.Cell(1, c + 1).Value = columnas[c].Titulo;
        }

        for (var f = 0; f < filas.Count; f++)
        {
            for (var c = 0; c < columnas.Count; c++)
            {
                var celda = hoja.Cell(f + 2, c + 1);
                switch (columnas[c].Valor(filas[f]))
                {
                    case null:
                        break;
                    case DateOnly fecha:
                        celda.Value = fecha.ToDateTime(TimeOnly.MinValue);
                        celda.Style.DateFormat.Format = "dd-mm-yyyy";
                        break;
                    case int entero:
                        celda.Value = entero;
                        break;
                    case decimal numero:
                        celda.Value = (double)numero;
                        break;
                    case bool logico:
                        celda.Value = logico ? "Sí" : "No";
                        break;
                    case var otro:
                        // Siempre como texto y neutralizado contra inyección de fórmulas.
                        celda.Value = TextoSeguroExcel.Neutralizar(otro.ToString()) ?? string.Empty;
                        break;
                }
            }
        }

        var rango = hoja.Range(1, 1, Math.Max(filas.Count + 1, 2), columnas.Count);
        var tabla = rango.CreateTable(nombreHoja);
        tabla.Theme = XLTableTheme.TableStyleMedium2;

        hoja.SheetView.FreezeRows(1);
        hoja.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        return stream.ToArray();
    }
}
