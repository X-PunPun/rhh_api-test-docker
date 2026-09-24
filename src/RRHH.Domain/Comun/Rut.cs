using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace RRHH.Domain.Comun;

/// <summary>
/// Value object del RUT chileno. Solo puede existir si el dígito verificador
/// (algoritmo módulo 11) es correcto. Se guarda normalizado como "12345678-5".
/// </summary>
public sealed record Rut
{
    public const int LargoMaximoNormalizado = 10; // 99999999-K

    public int Numero { get; }
    public char DigitoVerificador { get; }

    private Rut(int numero, char digitoVerificador)
    {
        Numero = numero;
        DigitoVerificador = digitoVerificador;
    }

    /// <summary>Formato normalizado para persistencia: "12345678-5".</summary>
    public string Valor => $"{Numero}-{DigitoVerificador}";

    /// <summary>Formato para mostrar: "12.345.678-5".</summary>
    public string Formateado =>
        $"{Numero.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.')}-{DigitoVerificador}";

    public static Rut Crear(string? valor)
    {
        return TryParse(valor, out var rut)
            ? rut
            : throw new ExcepcionDominio("El RUT ingresado no es válido.");
    }

    public static bool TryParse(string? valor, [NotNullWhen(true)] out Rut? rut)
    {
        rut = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var limpio = valor.Replace(".", string.Empty)
                          .Replace("-", string.Empty)
                          .Replace(" ", string.Empty)
                          .ToUpperInvariant();

        // Mínimo: 1 dígito + DV. Máximo: 8 dígitos + DV.
        if (limpio.Length is < 2 or > 9)
        {
            return false;
        }

        var cuerpo = limpio[..^1];
        var digito = limpio[^1];

        if (!int.TryParse(cuerpo, NumberStyles.None, CultureInfo.InvariantCulture, out var numero) || numero <= 0)
        {
            return false;
        }

        if (CalcularDigitoVerificador(numero) != digito)
        {
            return false;
        }

        rut = new Rut(numero, digito);
        return true;
    }

    public static char CalcularDigitoVerificador(int numero)
    {
        var suma = 0;
        var multiplicador = 2;

        while (numero > 0)
        {
            suma += numero % 10 * multiplicador;
            numero /= 10;
            multiplicador = multiplicador == 7 ? 2 : multiplicador + 1;
        }

        return (11 - suma % 11) switch
        {
            11 => '0',
            10 => 'K',
            var resto => (char)('0' + resto),
        };
    }

    public override string ToString() => Valor;
}
