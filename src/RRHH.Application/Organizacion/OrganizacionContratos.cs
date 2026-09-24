using System.ComponentModel.DataAnnotations;
using RRHH.Domain.Organizacion;

namespace RRHH.Application.Organizacion;

public sealed record DepartamentoDto(int Id, string Nombre, string? Descripcion, bool Activo, int EmpleadosActivos);

public sealed record CargoDto(int Id, string Nombre, int DepartamentoId, string Departamento, bool Activo);

public sealed record GuardarDepartamentoComando(
    [Required, StringLength(Departamento.LargoMaximoNombre)] string Nombre,
    [StringLength(Departamento.LargoMaximoDescripcion)] string? Descripcion);

public sealed record CrearCargoComando(
    [Required, StringLength(Cargo.LargoMaximoNombre)] string Nombre,
    [Range(1, int.MaxValue)] int DepartamentoId);

public sealed record RenombrarCargoComando(
    [Required, StringLength(Cargo.LargoMaximoNombre)] string Nombre);
