using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Ubicacion;
using RRHH.Infrastructure.Persistencia.Semillas;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class RegionConfiguracion : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("Regiones");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever(); // Id = código CUT
        builder.Property(r => r.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Abreviatura).HasMaxLength(5).IsUnicode(false).IsRequired();
        builder.HasIndex(r => r.Orden).IsUnique();

        builder.HasMany(r => r.Comunas)
               .WithOne(c => c.Region)
               .HasForeignKey(c => c.RegionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(r => r.Comunas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(DatosUbicacion.Regiones.Select(r => new
        {
            r.Id,
            r.Nombre,
            r.Abreviatura,
            r.Orden,
        }));
    }
}
