using System.ComponentModel.DataAnnotations;
using RRHH.Domain.Calendario;
using RRHH.Domain.Vacaciones;

namespace RRHH.Application.Vacaciones;

public sealed record SaldoVacacionesDto(
    int EmpleadoId,
    DateOnly FechaCalculo,
    int AniosServicio,
    int DiasProgresivosAnuales,
    decimal DiasDevengados,
    int DiasTomados,
    int DiasPendientesAprobacion,
    decimal DiasDisponibles);

public sealed record SolicitudVacacionesDto(
    int Id,
    int EmpleadoId,
    string Empleado,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    int DiasHabiles,
    EstadoSolicitud Estado,
    string? Comentario,
    DateTimeOffset FechaSolicitud,
    int? ResueltaPorId,
    DateTimeOffset? FechaResolucion,
    string? MotivoRechazo);

public sealed record SolicitarVacacionesComando(
    DateOnly FechaInicio,
    DateOnly FechaFin,
    [StringLength(SolicitudVacaciones.LargoMaximoTexto)] string? Comentario);

public sealed record RechazarSolicitudComando(
    [Required, StringLength(SolicitudVacaciones.LargoMaximoTexto)] string Motivo);

public sealed record FeriadoDto(int Id, DateOnly Fecha, string Nombre);

public sealed record CrearFeriadoComando(
    DateOnly Fecha,
    [Required, StringLength(Feriado.LargoMaximoNombre)] string Nombre);
