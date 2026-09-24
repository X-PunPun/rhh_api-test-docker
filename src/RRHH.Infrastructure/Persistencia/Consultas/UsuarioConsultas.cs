using Microsoft.EntityFrameworkCore;
using RRHH.Application.Comun;
using RRHH.Application.Seguridad;

namespace RRHH.Infrastructure.Persistencia.Consultas;

internal sealed class UsuarioConsultas(RrhhDbContext db) : IUsuarioConsultas
{
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(DateTimeOffset ahora, CancellationToken ct) =>
        await Proyectar(db.Usuarios.AsNoTracking().OrderBy(u => u.Email), ahora).ToListAsync(ct);

    public Task<UsuarioDto?> ObtenerAsync(int id, DateTimeOffset ahora, CancellationToken ct) =>
        Proyectar(db.Usuarios.AsNoTracking().Where(u => u.Id == id), ahora).FirstOrDefaultAsync(ct);

    public async Task<Pagina<RegistroAuditoriaDto>> ListarAuditoriaAsync(FiltroAuditoria filtro, CancellationToken ct)
    {
        var consulta = db.Auditoria.AsNoTracking();

        if (filtro.UsuarioId is { } usuarioId)
        {
            consulta = consulta.Where(r => r.UsuarioId == usuarioId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Accion))
        {
            consulta = consulta.Where(r => r.Accion.StartsWith(filtro.Accion));
        }

        if (filtro.Desde is { } desde)
        {
            consulta = consulta.Where(r => r.Fecha >= desde);
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(r => r.Id)
            .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
            .Take(filtro.TamanoPagina)
            .Select(r => new RegistroAuditoriaDto(r.Id, r.Fecha, r.UsuarioId, r.Email, r.Accion, r.Detalle, r.CodigoResultado, r.Ip))
            .ToListAsync(ct);

        return new Pagina<RegistroAuditoriaDto>(items, filtro.Pagina, filtro.TamanoPagina, total);
    }

    private IQueryable<UsuarioDto> Proyectar(IQueryable<Domain.Seguridad.Usuario> consulta, DateTimeOffset ahora) =>
        from u in consulta
        join e in db.Empleados on u.EmpleadoId equals e.Id into empleados
        from e in empleados.DefaultIfEmpty()
        select new UsuarioDto(
            u.Id,
            u.Email,
            u.Rol,
            u.EmpleadoId,
            e == null ? null : e.Nombres + " " + e.ApellidoPaterno,
            u.Regiones,
            u.Activo,
            u.BloqueadoHasta != null && u.BloqueadoHasta > ahora,
            u.UltimoAcceso);
}
