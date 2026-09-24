using System.ComponentModel.DataAnnotations;
using RRHH.Domain.Seguros;

namespace RRHH.Application.Seguros;

public sealed record PlanSeguroDto(int Id, string Nombre, string Aseguradora, TipoSeguro Tipo, decimal PrimaMensualUf, bool Activo);

public sealed record AfiliacionSeguroDto(
    int Id,
    int EmpleadoId,
    int PlanSeguroId,
    string Plan,
    string Aseguradora,
    TipoSeguro Tipo,
    decimal PrimaMensualUf,
    DateOnly FechaInicio,
    DateOnly? FechaTermino,
    int NumeroCargas,
    bool Vigente);

public sealed record GuardarPlanSeguroComando(
    [Required, StringLength(PlanSeguro.LargoMaximoNombre)] string Nombre,
    [Required, StringLength(PlanSeguro.LargoMaximoNombre)] string Aseguradora,
    TipoSeguro Tipo,
    [Range(typeof(decimal), "0", "1000")] decimal PrimaMensualUf);

public sealed record AfiliarSeguroComando(
    [Range(1, int.MaxValue)] int PlanSeguroId,
    DateOnly FechaInicio,
    [Range(0, AfiliacionSeguro.MaximoCargas)] int NumeroCargas);

public sealed record TerminarAfiliacionComando(DateOnly FechaTermino);
