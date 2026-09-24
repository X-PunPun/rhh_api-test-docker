namespace RRHH.Application.Comun;

/// <summary>Resultado paginado.</summary>
public sealed record Pagina<T>(IReadOnlyList<T> Items, int NumeroPagina, int TamanoPagina, int Total)
{
    public int TotalPaginas => TamanoPagina == 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanoPagina);
}
