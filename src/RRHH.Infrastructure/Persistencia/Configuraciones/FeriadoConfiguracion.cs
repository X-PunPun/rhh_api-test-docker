using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Calendario;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class FeriadoConfiguracion : IEntityTypeConfiguration<Feriado>
{
    public void Configure(EntityTypeBuilder<Feriado> builder)
    {
        builder.ToTable("Feriados");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Nombre).HasMaxLength(Feriado.LargoMaximoNombre).IsRequired();
        builder.HasIndex(f => f.Fecha).IsUnique();

        // Feriados legales de Chile 2026 (verificar anualmente en feriados.cl / gob.cl).
        builder.HasData(
            Dia(1, 2026, 1, 1, "Año Nuevo"),
            Dia(2, 2026, 4, 3, "Viernes Santo"),
            Dia(3, 2026, 4, 4, "Sábado Santo"),
            Dia(4, 2026, 5, 1, "Día Nacional del Trabajo"),
            Dia(5, 2026, 5, 21, "Día de las Glorias Navales"),
            Dia(6, 2026, 6, 21, "Día Nacional de los Pueblos Indígenas"),
            Dia(7, 2026, 6, 29, "San Pedro y San Pablo"),
            Dia(8, 2026, 7, 16, "Día de la Virgen del Carmen"),
            Dia(9, 2026, 8, 15, "Asunción de la Virgen"),
            Dia(10, 2026, 9, 18, "Independencia Nacional"),
            Dia(11, 2026, 9, 19, "Día de las Glorias del Ejército"),
            Dia(12, 2026, 10, 12, "Encuentro de Dos Mundos"),
            Dia(13, 2026, 10, 31, "Día de las Iglesias Evangélicas y Protestantes"),
            Dia(14, 2026, 11, 1, "Día de Todos los Santos"),
            Dia(15, 2026, 12, 8, "Inmaculada Concepción"),
            Dia(16, 2026, 12, 25, "Navidad"));
    }

    private static object Dia(int id, int anio, int mes, int dia, string nombre) =>
        new { Id = id, Fecha = new DateOnly(anio, mes, dia), Nombre = nombre };
}
