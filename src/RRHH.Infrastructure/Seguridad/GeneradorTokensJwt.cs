using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RRHH.Application.Seguridad;
using RRHH.Domain.Seguridad;

namespace RRHH.Infrastructure.Seguridad;

/// <summary>Adaptador: emite JWT firmados con HMAC-SHA256 y tokens de renovación aleatorios.</summary>
internal sealed class GeneradorTokensJwt(IOptions<OpcionesJwt> opciones) : IGeneradorTokens
{
    public const string ClaimEmpleadoId = "empleado_id";
    public const string ClaimRegion = "region";
    public const string ClaimRol = "role";

    private readonly OpcionesJwt _opciones = opciones.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public TimeSpan VigenciaRenovacion => TimeSpan.FromDays(_opciones.DiasRenovacion);

    public TokenAcceso GenerarAcceso(Usuario usuario, DateTimeOffset ahora)
    {
        var expira = ahora.AddMinutes(_opciones.MinutosAcceso);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimRol, usuario.Rol.ToString()),
        };

        if (usuario.EmpleadoId is { } empleadoId)
        {
            claims.Add(new Claim(ClaimEmpleadoId, empleadoId.ToString(CultureInfo.InvariantCulture)));
        }

        claims.AddRange(usuario.Regiones.Select(r => new Claim(ClaimRegion, r.ToString(CultureInfo.InvariantCulture))));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opciones.Emisor,
            Audience = _opciones.Audiencia,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_opciones.LlaveBytes()), SecurityAlgorithms.HmacSha256),
        };

        return new TokenAcceso(_handler.CreateToken(descriptor), expira);
    }

    public string GenerarTokenRenovacion() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    public string CalcularHash(string token) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
