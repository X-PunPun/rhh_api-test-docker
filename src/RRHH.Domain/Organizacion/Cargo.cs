using RRHH.Domain.Comun;

namespace RRHH.Domain.Organizacion;

/// <summary>Cargo o puesto de trabajo dentro de un departamento.</summary>
public sealed class Cargo : Entidad<int>
{
    public const int LargoMaximoNombre = 100;

    public string Nombre { get; private set; } = null!;
    public int DepartamentoId { get; private set; }
    public Departamento Departamento { get; private set; } = null!;
    public bool Activo { get; private set; }

    private Cargo() { } // EF Core

    public static Cargo Crear(string nombre, int departamentoId)
    {
        return new Cargo
        {
            Nombre = Guardia.TextoRequerido(nombre, nameof(Nombre), LargoMaximoNombre),
            DepartamentoId = Guardia.IdPositivo(departamentoId, nameof(DepartamentoId)),
            Activo = true,
        };
    }

    public void Renombrar(string nombre) =>
        Nombre = Guardia.TextoRequerido(nombre, nameof(Nombre), LargoMaximoNombre);

    public void Desactivar() => Activo = false;
}
