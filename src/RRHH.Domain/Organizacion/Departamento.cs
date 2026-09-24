using RRHH.Domain.Comun;

namespace RRHH.Domain.Organizacion;

public sealed class Departamento : Entidad<int>
{
    public const int LargoMaximoNombre = 100;
    public const int LargoMaximoDescripcion = 500;

    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public bool Activo { get; private set; }

    private Departamento() { } // EF Core

    public static Departamento Crear(string nombre, string? descripcion)
    {
        return new Departamento
        {
            Nombre = Guardia.TextoRequerido(nombre, nameof(Nombre), LargoMaximoNombre),
            Descripcion = Guardia.TextoOpcional(descripcion, nameof(Descripcion), LargoMaximoDescripcion),
            Activo = true,
        };
    }

    public void Actualizar(string nombre, string? descripcion)
    {
        Nombre = Guardia.TextoRequerido(nombre, nameof(Nombre), LargoMaximoNombre);
        Descripcion = Guardia.TextoOpcional(descripcion, nameof(Descripcion), LargoMaximoDescripcion);
    }

    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;
}
