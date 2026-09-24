namespace RRHH.Application.Comun;

/// <summary>
/// Puerto que expone la identidad de quien ejecuta la operación.
/// Hoy lo implementa un adaptador de desarrollo (cabecera X-Empleado-Id);
/// en la fase de seguridad se reemplaza por uno que lee los claims del JWT,
/// sin tocar los casos de uso.
/// </summary>
public interface IUsuarioActual
{
    int? EmpleadoId { get; }
}
