namespace RRHH.Domain.Comun;

/// <summary>
/// Se lanza cuando se viola una regla de negocio del dominio.
/// La API la traduce a un 400 (ProblemDetails) sin exponer detalles internos.
/// </summary>
public sealed class ExcepcionDominio(string mensaje) : Exception(mensaje);
