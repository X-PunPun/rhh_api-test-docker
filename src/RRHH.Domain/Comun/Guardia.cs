namespace RRHH.Domain.Comun;

/// <summary>Validaciones reutilizables para invariantes del dominio.</summary>
internal static class Guardia
{
    public static string TextoRequerido(string? valor, string campo, int largoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ExcepcionDominio($"El campo '{campo}' es obligatorio.");
        }

        var limpio = valor.Trim();
        if (limpio.Length > largoMaximo)
        {
            throw new ExcepcionDominio($"El campo '{campo}' no puede superar {largoMaximo} caracteres.");
        }

        return limpio;
    }

    public static string? TextoOpcional(string? valor, string campo, int largoMaximo)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : TextoRequerido(valor, campo, largoMaximo);
    }

    public static int IdPositivo(int valor, string campo)
    {
        if (valor <= 0)
        {
            throw new ExcepcionDominio($"El campo '{campo}' debe ser un identificador válido.");
        }

        return valor;
    }
}
