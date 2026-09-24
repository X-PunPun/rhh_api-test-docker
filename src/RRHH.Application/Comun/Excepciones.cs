namespace RRHH.Application.Comun;

/// <summary>El recurso solicitado no existe (→ 404).</summary>
public sealed class RecursoNoEncontradoException(string recurso, object id)
    : Exception($"{recurso} con id '{id}' no existe.");

/// <summary>La operación choca con el estado actual: duplicados, concurrencia, superposición (→ 409).</summary>
public sealed class ConflictoException(string mensaje) : Exception(mensaje);

/// <summary>El usuario actual no puede realizar la operación sobre ese recurso (→ 403).</summary>
public sealed class AccesoDenegadoException(string mensaje) : Exception(mensaje);
