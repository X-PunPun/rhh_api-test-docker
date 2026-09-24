using RRHH.Application.Comun;
using RRHH.Application.Seguridad;
using RRHH.Domain.Calendario;

namespace RRHH.Application.Vacaciones;

public interface IFeriadoServicio
{
    Task<IReadOnlyList<FeriadoDto>> ListarAsync(int anio, CancellationToken ct);
    Task<FeriadoDto> CrearAsync(CrearFeriadoComando comando, CancellationToken ct);
    Task EliminarAsync(int id, CancellationToken ct);
}

internal sealed class FeriadoServicio(
    IFeriadoRepositorio feriados,
    IVacacionesConsultas consultas,
    IUsuarioActual usuarioActual,
    IUnidadDeTrabajo unidadDeTrabajo) : IFeriadoServicio
{
    public Task<IReadOnlyList<FeriadoDto>> ListarAsync(int anio, CancellationToken ct) =>
        consultas.ListarFeriadosAsync(anio, ct);

    public async Task<FeriadoDto> CrearAsync(CrearFeriadoComando comando, CancellationToken ct)
    {
        usuarioActual.ExigirGestor();
        if (await feriados.ExisteFechaAsync(comando.Fecha, ct))
        {
            throw new ConflictoException($"Ya existe un feriado el {comando.Fecha:dd-MM-yyyy}.");
        }

        var feriado = Feriado.Crear(comando.Fecha, comando.Nombre);
        feriados.Agregar(feriado);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);

        return new FeriadoDto(feriado.Id, feriado.Fecha, feriado.Nombre);
    }

    public async Task EliminarAsync(int id, CancellationToken ct)
    {
        usuarioActual.ExigirGestor();
        var feriado = await feriados.ObtenerAsync(id, ct) ?? throw new RecursoNoEncontradoException("Feriado", id);
        feriados.Eliminar(feriado);
        await unidadDeTrabajo.GuardarCambiosAsync(ct);
    }
}
