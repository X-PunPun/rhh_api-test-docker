using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRHH.Api.Seguridad;
using RRHH.Application.Comun;
using RRHH.Application.Seguridad;

namespace RRHH.Api.Controllers.V1;

/// <summary>Administración de usuarios, roles y bitácora de auditoría (solo Admin).</summary>
[ApiController]
[Route("api/v1/usuarios")]
[Authorize(Policy = Politicas.Admin)]
[Produces("application/json")]
public sealed class UsuariosController(IUsuariosServicio usuarios) : ControllerBase
{
    /// <summary>Lista los usuarios.</summary>
    [HttpGet]
    public Task<IReadOnlyList<UsuarioDto>> Listar(CancellationToken ct) => usuarios.ListarAsync(ct);

    /// <summary>Obtiene un usuario.</summary>
    [HttpGet("{id:int}", Name = "ObtenerUsuario")]
    public Task<UsuarioDto> Obtener(int id, CancellationToken ct) => usuarios.ObtenerAsync(id, ct);

    /// <summary>Crea un usuario (normalmente para un empleado) con rol y clave inicial.</summary>
    [HttpPost]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Crear(CrearUsuarioComando comando, CancellationToken ct)
    {
        var dto = await usuarios.CrearAsync(comando, ct);
        return CreatedAtRoute("ObtenerUsuario", new { id = dto.Id }, dto);
    }

    /// <summary>Cambia el rol (y regiones, si es RRHH). Cierra las sesiones del usuario.</summary>
    [HttpPut("{id:int}/rol")]
    public Task<UsuarioDto> AsignarRol(int id, AsignarRolComando comando, CancellationToken ct) =>
        usuarios.AsignarRolAsync(id, comando, ct);

    /// <summary>Activa un usuario.</summary>
    [HttpPost("{id:int}/activar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        await usuarios.CambiarEstadoAsync(id, activo: true, ct);
        return NoContent();
    }

    /// <summary>Desactiva un usuario y cierra sus sesiones.</summary>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        await usuarios.CambiarEstadoAsync(id, activo: false, ct);
        return NoContent();
    }

    /// <summary>Quita el bloqueo por intentos fallidos.</summary>
    [HttpPost("{id:int}/desbloquear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desbloquear(int id, CancellationToken ct)
    {
        await usuarios.DesbloquearAsync(id, ct);
        return NoContent();
    }

    /// <summary>Asigna una clave nueva (por ejemplo, si el usuario la olvidó). Cierra sus sesiones.</summary>
    [HttpPost("{id:int}/restablecer-clave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RestablecerClave(int id, RestablecerClaveComando comando, CancellationToken ct)
    {
        await usuarios.RestablecerClaveAsync(id, comando, ct);
        return NoContent();
    }

    /// <summary>Bitácora de auditoría (más recientes primero).</summary>
    [HttpGet("/api/v1/auditoria")]
    public Task<Pagina<RegistroAuditoriaDto>> Auditoria([FromQuery] FiltroAuditoria filtro, CancellationToken ct) =>
        usuarios.ListarAuditoriaAsync(filtro, ct);
}
