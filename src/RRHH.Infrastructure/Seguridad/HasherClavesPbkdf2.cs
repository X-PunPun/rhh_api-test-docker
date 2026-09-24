using Microsoft.AspNetCore.Identity;
using RRHH.Application.Seguridad;
using RRHH.Domain.Seguridad;

namespace RRHH.Infrastructure.Seguridad;

/// <summary>
/// Adaptador sobre el PasswordHasher de ASP.NET Core Identity (PBKDF2 con HMAC-SHA512,
/// sal aleatoria e iteraciones altas). Se reutiliza el algoritmo probado sin adoptar todo Identity.
/// </summary>
internal sealed class HasherClavesPbkdf2 : IHasherClaves
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hashear(string clave) => _hasher.HashPassword(null!, clave);

    public bool Verificar(string hash, string clave)
    {
        try
        {
            return _hasher.VerifyHashedPassword(null!, hash, clave) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
