using RRHH.Domain.Comun;

namespace RRHH.Application.Seguridad;

/// <summary>Requisitos mínimos de una clave nueva.</summary>
public static class PoliticaClaves
{
    public const int LargoMinimo = 10;

    public static void Validar(string clave, string? email = null)
    {
        var errores = new List<string>();

        if (string.IsNullOrEmpty(clave) || clave.Length < LargoMinimo)
        {
            errores.Add($"al menos {LargoMinimo} caracteres");
        }

        if (!clave.Any(char.IsUpper))
        {
            errores.Add("una mayúscula");
        }

        if (!clave.Any(char.IsLower))
        {
            errores.Add("una minúscula");
        }

        if (!clave.Any(char.IsDigit))
        {
            errores.Add("un número");
        }

        if (clave.All(char.IsLetterOrDigit))
        {
            errores.Add("un símbolo");
        }

        var usuario = email?.Split('@')[0];
        if (!string.IsNullOrEmpty(usuario) && usuario.Length >= 3 &&
            clave.Contains(usuario, StringComparison.OrdinalIgnoreCase))
        {
            errores.Add("no contener el nombre de usuario");
        }

        if (errores.Count > 0)
        {
            throw new ExcepcionDominio($"La clave debe tener {string.Join(", ", errores)}.");
        }
    }
}
