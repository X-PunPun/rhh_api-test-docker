using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Seguros;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class PlanSeguroConfiguracion : IEntityTypeConfiguration<PlanSeguro>
{
    public void Configure(EntityTypeBuilder<PlanSeguro> builder)
    {
        builder.ToTable("PlanesSeguro");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nombre).HasMaxLength(PlanSeguro.LargoMaximoNombre).IsRequired();
        builder.Property(p => p.Aseguradora).HasMaxLength(PlanSeguro.LargoMaximoNombre).IsRequired();
        builder.Property(p => p.Tipo).IsUnicode(false);
        builder.Property(p => p.PrimaMensualUf).HasPrecision(10, 4);
        builder.HasIndex(p => new { p.Aseguradora, p.Nombre }).IsUnique();
    }
}
