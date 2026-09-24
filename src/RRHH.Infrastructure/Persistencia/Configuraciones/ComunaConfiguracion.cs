using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Ubicacion;
using RRHH.Infrastructure.Persistencia.Semillas;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class ComunaConfiguracion : IEntityTypeConfiguration<Comuna>
{
    public void Configure(EntityTypeBuilder<Comuna> builder)
    {
        builder.ToTable("Comunas");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever(); // Id = código CUT
        builder.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => new { c.RegionId, c.Nombre }).IsUnique();

        builder.HasData(DatosUbicacion.Comunas.Select(c => new
        {
            c.Id,
            c.Nombre,
            c.RegionId,
        }));
    }
}
