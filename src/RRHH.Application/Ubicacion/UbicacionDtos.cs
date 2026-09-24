namespace RRHH.Application.Ubicacion;

public sealed record RegionDto(int Id, string Nombre, string Abreviatura);

public sealed record ComunaDto(int Id, string Nombre, int RegionId);
