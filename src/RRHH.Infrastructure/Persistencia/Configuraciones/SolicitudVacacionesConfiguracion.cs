using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Vacaciones;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class SolicitudVacacionesConfiguracion : IEntityTypeConfiguration<SolicitudVacaciones>
{
    public void Configure(EntityTypeBuilder<SolicitudVacaciones> builder)
    {
        builder.ToTable("SolicitudesVacaciones");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Estado).IsUnicode(false);
        builder.Property(s => s.Comentario).HasMaxLength(SolicitudVacaciones.LargoMaximoTexto);
        builder.Property(s => s.MotivoRechazo).HasMaxLength(SolicitudVacaciones.LargoMaximoTexto);

        builder.Property<byte[]>("Version").IsRowVersion();

        builder.HasOne(s => s.Empleado).WithMany()
               .HasForeignKey(s => s.EmpleadoId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Empleados.Empleado>().WithMany()
               .HasForeignKey(s => s.ResueltaPorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.EmpleadoId, s.Estado });
    }
}
