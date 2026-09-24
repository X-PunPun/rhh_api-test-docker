using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Organizacion;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class CargoConfiguracion : IEntityTypeConfiguration<Cargo>
{
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        builder.ToTable("Cargos");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nombre).HasMaxLength(Cargo.LargoMaximoNombre).IsRequired();
        builder.HasIndex(c => new { c.DepartamentoId, c.Nombre }).IsUnique();

        builder.HasOne(c => c.Departamento)
               .WithMany()
               .HasForeignKey(c => c.DepartamentoId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
