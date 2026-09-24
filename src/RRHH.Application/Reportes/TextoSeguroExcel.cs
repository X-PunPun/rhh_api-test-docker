namespace RRHH.Application.Reportes;

/// <summary>
/// Previene la inyección de fórmulas (CSV/Formula Injection): un texto que empieza con
/// =, +, -, @, tabulación o retorno de carro podría ser interpretado como fórmula por
/// Excel u otras planillas al reabrir o convertir el archivo. Se antepone un apóstrofo.
/// </summary>
public static class TextoSeguroExcel
{
    private static readonly char[] CaracteresPeligrosos = ['=', '+', '-', '@', '\t', '\r'];

    public static string? Neutralizar(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return valor;
        }

        return Array.IndexOf(CaracteresPeligrosos, valor[0]) >= 0 ? "'" + valor : valor;
    }
}
