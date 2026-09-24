namespace RRHH.Api.OpenApi;

/// <summary>
/// Marca endpoints que usan la identidad del usuario actual (hoy vía cabecera X-Empleado-Id).
/// Solo sirve para que Swagger/Scalar muestren el campo de la cabecera.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiereIdentidadDemoAttribute : Attribute;
