using System.ComponentModel.DataAnnotations;
using RRHH.Application.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Application.Empleados;

public sealed record EmpleadoResumenDto(
    int Id,
    string Rut,
    string NombreCompleto,
    string Email,
    string Cargo,
    string Departamento,
    string Region,
    string Comuna,
    string? Jefe,
    DateOnly FechaIngreso,
    bool Activo);

public sealed record EmpleadoDetalleDto(
    int Id,
    string Rut,
    string Nombres,
    string ApellidoPaterno,
    string? ApellidoMaterno,
    string Email,
    DateOnly FechaNacimiento,
    DateOnly FechaIngreso,
    DateOnly? FechaTermino,
    bool Activo,
    ReferenciaDto Departamento,
    ReferenciaDto Cargo,
    ReferenciaDto Region,
    ReferenciaDto Comuna,
    ReferenciaDto? Jefe,
    Afp? Afp,
    SistemaSalud? SistemaSalud,
    int? AniosServicioPrevios,
    int CantidadSubordinados);

/// <summary>Filtros de búsqueda de empleados (todos opcionales).</summary>
public sealed record FiltroEmpleados
{
    public const int TamanoPaginaMaximo = 100;

    public int? RegionId { get; init; }
    public int? ComunaId { get; init; }
    public int? DepartamentoId { get; init; }
    public int? CargoId { get; init; }
    public int? JefeId { get; init; }
    public bool? Activo { get; init; }

    /// <summary>Busca en nombre, apellidos, email o RUT.</summary>
    [StringLength(100)]
    public string? Busqueda { get; init; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, TamanoPaginaMaximo)]
    public int TamanoPagina { get; init; } = 20;
}

public sealed record CrearEmpleadoComando(
    [Required, StringLength(12)] string Rut,
    [Required, StringLength(Empleado.LargoMaximoNombre)] string Nombres,
    [Required, StringLength(Empleado.LargoMaximoNombre)] string ApellidoPaterno,
    [StringLength(Empleado.LargoMaximoNombre)] string? ApellidoMaterno,
    [Required, EmailAddress, StringLength(Empleado.LargoMaximoEmail)] string Email,
    DateOnly FechaNacimiento,
    DateOnly FechaIngreso,
    [Range(1, int.MaxValue)] int DepartamentoId,
    [Range(1, int.MaxValue)] int CargoId,
    [Range(1, int.MaxValue)] int ComunaId,
    int? JefeId,
    Afp Afp,
    SistemaSalud SistemaSalud,
    [Range(0, 60)] int AniosServicioPrevios = 0);

public sealed record ActualizarEmpleadoComando(
    [Required, EmailAddress, StringLength(Empleado.LargoMaximoEmail)] string Email,
    [Range(1, int.MaxValue)] int ComunaId,
    [Range(1, int.MaxValue)] int DepartamentoId,
    [Range(1, int.MaxValue)] int CargoId,
    int? JefeId,
    Afp Afp,
    SistemaSalud SistemaSalud,
    [Range(0, 60)] int AniosServicioPrevios);

public sealed record DesvincularEmpleadoComando(DateOnly FechaTermino);
