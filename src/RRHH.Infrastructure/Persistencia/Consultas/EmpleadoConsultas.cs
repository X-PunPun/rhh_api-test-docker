using Microsoft.EntityFrameworkCore;
using RRHH.Application.Comun;
using RRHH.Application.Empleados;
using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Infrastructure.Persistencia.Consultas;

internal sealed class EmpleadoConsultas(RrhhDbContext db) : IEmpleadoConsultas
{
    public async Task<Pagina<EmpleadoResumenDto>> BuscarAsync(FiltroEmpleados filtro, CancellationToken ct)
    {
        var consulta = AplicarFiltro(db.Empleados.AsNoTracking(), filtro);
        var total = await consulta.CountAsync(ct);

        var filas = await Proyectar(Ordenar(consulta)
                .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
                .Take(filtro.TamanoPagina))
            .ToListAsync(ct);

        return new Pagina<EmpleadoResumenDto>(filas.Select(AResumen).ToList(), filtro.Pagina, filtro.TamanoPagina, total);
    }

    public async Task<IReadOnlyList<EmpleadoResumenDto>> ListarParaExportarAsync(
        FiltroEmpleados filtro, int maximo, CancellationToken ct)
    {
        var filas = await Proyectar(Ordenar(AplicarFiltro(db.Empleados.AsNoTracking(), filtro)).Take(maximo + 1))
            .ToListAsync(ct);

        return filas.Select(AResumen).ToList();
    }

    public async Task<IReadOnlyList<EmpleadoResumenDto>> ListarSubordinadosAsync(int jefeId, CancellationToken ct)
    {
        var filas = await Proyectar(Ordenar(db.Empleados.AsNoTracking().Where(e => e.JefeId == jefeId)))
            .ToListAsync(ct);

        return filas.Select(AResumen).ToList();
    }

    public async Task<EmpleadoDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct)
    {
        var fila = await db.Empleados.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Rut,
                e.Nombres,
                e.ApellidoPaterno,
                e.ApellidoMaterno,
                e.Email,
                e.FechaNacimiento,
                e.FechaIngreso,
                e.FechaTermino,
                Departamento = new ReferenciaDto(e.DepartamentoId, e.Departamento.Nombre),
                Cargo = new ReferenciaDto(e.CargoId, e.Cargo.Nombre),
                Region = new ReferenciaDto(e.Comuna.RegionId, e.Comuna.Region.Nombre),
                Comuna = new ReferenciaDto(e.ComunaId, e.Comuna.Nombre),
                JefeId = e.JefeId,
                JefeNombre = e.Jefe == null ? null : e.Jefe.Nombres + " " + e.Jefe.ApellidoPaterno,
                e.Afp,
                e.SistemaSalud,
                e.AniosServicioPrevios,
                Subordinados = db.Empleados.Count(s => s.JefeId == e.Id && s.FechaTermino == null),
            })
            .FirstOrDefaultAsync(ct);

        if (fila is null)
        {
            return null;
        }

        return new EmpleadoDetalleDto(
            fila.Id,
            fila.Rut.Formateado,
            fila.Nombres,
            fila.ApellidoPaterno,
            fila.ApellidoMaterno,
            fila.Email,
            fila.FechaNacimiento,
            fila.FechaIngreso,
            fila.FechaTermino,
            fila.FechaTermino is null,
            fila.Departamento,
            fila.Cargo,
            fila.Region,
            fila.Comuna,
            fila.JefeId is { } jefeId ? new ReferenciaDto(jefeId, fila.JefeNombre ?? string.Empty) : null,
            fila.Afp,
            fila.SistemaSalud,
            fila.AniosServicioPrevios,
            fila.Subordinados);
    }

    private static IQueryable<Empleado> AplicarFiltro(IQueryable<Empleado> consulta, FiltroEmpleados filtro)
    {
        if (filtro.RegionId is { } regionId)
        {
            consulta = consulta.Where(e => e.Comuna.RegionId == regionId);
        }

        if (filtro.ComunaId is { } comunaId)
        {
            consulta = consulta.Where(e => e.ComunaId == comunaId);
        }

        if (filtro.DepartamentoId is { } departamentoId)
        {
            consulta = consulta.Where(e => e.DepartamentoId == departamentoId);
        }

        if (filtro.CargoId is { } cargoId)
        {
            consulta = consulta.Where(e => e.CargoId == cargoId);
        }

        if (filtro.JefeId is { } jefeId)
        {
            consulta = consulta.Where(e => e.JefeId == jefeId);
        }

        if (filtro.Activo is { } activo)
        {
            consulta = activo ? consulta.Where(e => e.FechaTermino == null) : consulta.Where(e => e.FechaTermino != null);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var texto = filtro.Busqueda.Trim();

            if (Rut.TryParse(texto, out var rut))
            {
                consulta = consulta.Where(e => e.Rut == rut);
            }
            else
            {
                // Parámetro de EF (consulta parametrizada): no hay concatenación de SQL.
                consulta = consulta.Where(e =>
                    e.Nombres.Contains(texto) ||
                    e.ApellidoPaterno.Contains(texto) ||
                    (e.ApellidoMaterno != null && e.ApellidoMaterno.Contains(texto)) ||
                    e.Email.Contains(texto));
            }
        }

        return consulta;
    }

    private static IQueryable<Empleado> Ordenar(IQueryable<Empleado> consulta) =>
        consulta.OrderBy(e => e.ApellidoPaterno).ThenBy(e => e.Nombres).ThenBy(e => e.Id);

    private static IQueryable<FilaResumen> Proyectar(IQueryable<Empleado> consulta) =>
        consulta.Select(e => new FilaResumen(
            e.Id,
            e.Rut,
            e.Nombres,
            e.ApellidoPaterno,
            e.ApellidoMaterno,
            e.Email,
            e.Cargo.Nombre,
            e.Departamento.Nombre,
            e.Comuna.Region.Nombre,
            e.Comuna.Nombre,
            e.Jefe == null ? null : e.Jefe.Nombres + " " + e.Jefe.ApellidoPaterno,
            e.FechaIngreso,
            e.FechaTermino));

    private static EmpleadoResumenDto AResumen(FilaResumen f) => new(
        f.Id,
        f.Rut.Formateado,
        string.Join(' ', new[] { f.Nombres, f.ApellidoPaterno, f.ApellidoMaterno }.Where(p => !string.IsNullOrWhiteSpace(p))),
        f.Email,
        f.Cargo,
        f.Departamento,
        f.Region,
        f.Comuna,
        f.Jefe,
        f.FechaIngreso,
        f.FechaTermino is null);

    private sealed record FilaResumen(
        int Id,
        Rut Rut,
        string Nombres,
        string ApellidoPaterno,
        string? ApellidoMaterno,
        string Email,
        string Cargo,
        string Departamento,
        string Region,
        string Comuna,
        string? Jefe,
        DateOnly FechaIngreso,
        DateOnly? FechaTermino);
}
