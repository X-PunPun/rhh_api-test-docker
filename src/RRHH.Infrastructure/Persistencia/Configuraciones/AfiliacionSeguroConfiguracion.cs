using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Seguros;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class AfiliacionSeguroConfiguracion : IEntityTypeConfiguration<AfiliacionSeguro>
{
    public void Configure(EntityTypeBuilder<AfiliacionSeguro> builder)
    {
        builder.ToTable("AfiliacionesSeguro");
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Empleado).WithMany()
               .HasForeignKey(a => a.EmpleadoId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.PlanSeguro).WithMany()
               .HasForeignKey(a => a.PlanSeguroId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.EmpleadoId, a.PlanSeguroId });
    }
}
