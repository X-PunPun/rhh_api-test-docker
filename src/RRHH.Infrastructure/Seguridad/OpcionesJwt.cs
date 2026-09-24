using System.Text;

namespace RRHH.Infrastructure.Seguridad;

/// <summary>Configuración de tokens (sección "Jwt"). La llave se define con user-secrets o variables de entorno.</summary>
public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";
    public const int BytesMinimosLlave = 32;

    public string Emisor { get; set; } = "rrhh-api";
    public string Audiencia { get; set; } = "rrhh-front";
    public string Llave { get; set; } = string.Empty;
    public int MinutosAcceso { get; set; } = 15;
    public int DiasRenovacion { get; set; } = 7;

    public byte[] LlaveBytes() => Encoding.UTF8.GetBytes(Llave);

    public void Validar()
    {
        if (LlaveBytes().Length < BytesMinimosLlave)
        {
            throw new InvalidOperationException(
                $"Falta 'Jwt:Llave' o tiene menos de {BytesMinimosLlave} bytes. Configúrala con: " +
                "dotnet user-secrets set \"Jwt:Llave\" \"<cadena aleatoria de 64 caracteres>\" --project src/RRHH.Api");
        }

        if (MinutosAcceso is < 1 or > 60 || DiasRenovacion is < 1 or > 30)
        {
            throw new InvalidOperationException("Jwt:MinutosAcceso debe estar entre 1 y 60 y Jwt:DiasRenovacion entre 1 y 30.");
        }
    }
}
