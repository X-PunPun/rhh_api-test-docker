namespace RRHH.Domain.Vacaciones;

/// <summary>Resumen del saldo de feriado legal de un empleado a una fecha.</summary>
public sealed record SaldoVacaciones(decimal Devengados, int Tomados, int Pendientes)
{
    /// <summary>Días que aún puede solicitar (descuenta aprobadas y pendientes).</summary>
    public decimal Disponibles => Devengados - Tomados - Pendientes;
}
