using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class EmpleadoConfiguracion : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        builder.ToTable("Empleados");
        builder.HasKey(e => e.Id);

        // Value object Rut <-> columna varchar(10) "12345678-5"
        builder.Property(e => e.Rut)
               .HasConversion(rut => rut.Valor, valor => Rut.Crear(valor))
               .HasMaxLength(Rut.LargoMaximoNormalizado)
               .IsUnicode(false)
               .IsRequired();
        builder.HasIndex(e => e.Rut).IsUnique();

        builder.Property(e => e.Nombres).HasMaxLength(Empleado.LargoMaximoNombre).IsRequired();
        builder.Property(e => e.ApellidoPaterno).HasMaxLength(Empleado.LargoMaximoNombre).IsRequired();
        builder.Property(e => e.ApellidoMaterno).HasMaxLength(Empleado.LargoMaximoNombre);
        builder.Property(e => e.Email).HasMaxLength(Empleado.LargoMaximoEmail).IsRequired();
        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(e => e.Afp).IsUnicode(false);
        builder.Property(e => e.SistemaSalud).IsUnicode(false);

        builder.Ignore(e => e.Activo);
        builder.Ignore(e => e.NombreCompleto);

        // Control de concurrencia optimista (evita "lost updates" entre dos usuarios de RR.HH.)
        builder.Property<byte[]>("Version").IsRowVersion();

        builder.HasOne(e => e.Departamento).WithMany()
               .HasForeignKey(e => e.DepartamentoId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Cargo).WithMany()
               .HasForeignKey(e => e.CargoId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Comuna).WithMany()
               .HasForeignKey(e => e.ComunaId).OnDelete(DeleteBehavior.Restrict);

        // Jerarquía jefe -> subordinados (auto-referencia)
        builder.HasOne(e => e.Jefe).WithMany()
               .HasForeignKey(e => e.JefeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.JefeId);
        builder.HasIndex(e => new { e.DepartamentoId, e.FechaTermino });
    }
}
