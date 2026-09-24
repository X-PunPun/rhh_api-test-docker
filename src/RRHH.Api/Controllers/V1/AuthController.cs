using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RRHH.Api.Seguridad;
using RRHH.Application.Seguridad;

namespace RRHH.Api.Controllers.V1;

/// <summary>Inicio y cierre de sesión, renovación de tokens y perfil del usuario.</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(IAutenticacionServicio autenticacion) : ControllerBase
{
    /// <summary>Inicia sesión con email y clave. Devuelve un token de acceso (15 min) y uno de renovación.</summary>
    /// <remarks>Tras 5 intentos fallidos la cuenta se bloquea 15 minutos. Limitado a pocas solicitudes por minuto por IP.</remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesDeUso.PoliticaLogin)]
    [ProducesResponseType<TokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<TokensDto> Login(LoginComando comando, CancellationToken ct) =>
        autenticacion.IniciarSesionAsync(comando, ct);

    /// <summary>Entrega un nuevo par de tokens a cambio de un token de renovación válido (el anterior queda revocado).</summary>
    [HttpPost("renovar")]
    [AllowAnonymous]
    [EnableRateLimiting(LimitesDeUso.PoliticaLogin)]
    [ProducesResponseType<TokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<TokensDto> Renovar(RenovarTokenComando comando, CancellationToken ct) =>
        autenticacion.RenovarAsync(comando, ct);

    /// <summary>Cierra la sesión revocando el token de renovación.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RenovarTokenComando comando, CancellationToken ct)
    {
        await autenticacion.CerrarSesionAsync(comando, ct);
        return NoContent();
    }

    /// <summary>Datos del usuario autenticado (rol, empleado, regiones).</summary>
    [HttpGet("yo")]
    public Task<PerfilDto> Perfil(CancellationToken ct) => autenticacion.ObtenerPerfilAsync(ct);

    /// <summary>Cambia la clave propia. Cierra todas las demás sesiones.</summary>
    [HttpPost("cambiar-clave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CambiarClave(CambiarClaveComando comando, CancellationToken ct)
    {
        await autenticacion.CambiarClaveAsync(comando, ct);
        return NoContent();
    }
}
