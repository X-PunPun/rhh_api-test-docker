using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Empleados;
using RRHH.Domain.Seguridad;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios", "seguridad");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(Usuario.LargoMaximoEmail).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.HashClave).HasMaxLength(1000).IsRequired();
        builder.Property(u => u.Rol).IsUnicode(false);
        builder.Property(u => u.SelloSeguridad).HasMaxLength(64).IsUnicode(false).IsRequired();

        // Lista de regiones como colección primitiva (columna JSON) mapeada al campo privado.
        builder.PrimitiveCollection(u => u.Regiones)
               .HasField("_regiones")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Un empleado tiene como máximo un usuario.
        builder.HasIndex(u => u.EmpleadoId).IsUnique().HasFilter("[EmpleadoId] IS NOT NULL");
        builder.HasOne<Empleado>().WithMany()
               .HasForeignKey(u => u.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TokenRenovacionConfiguracion : IEntityTypeConfiguration<TokenRenovacion>
{
    public void Configure(EntityTypeBuilder<TokenRenovacion> builder)
    {
        builder.ToTable("TokensRenovacion", "seguridad");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Hash).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.HasIndex(t => t.Hash).IsUnique();
        builder.Property(t => t.SelloSeguridad).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(t => t.MotivoRevocacion).HasMaxLength(100);
        builder.Ignore(t => t.EstaRevocado);

        builder.HasOne<Usuario>().WithMany()
               .HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => new { t.UsuarioId, t.RevocadoEn });
    }
}

internal sealed class RegistroAuditoriaConfiguracion : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("Auditoria", "seguridad");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Email).HasMaxLength(150);
        builder.Property(r => r.Accion).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Detalle).HasMaxLength(500);
        builder.Property(r => r.Ip).HasMaxLength(64).IsUnicode(false);
        builder.HasIndex(r => r.Fecha);
        builder.HasIndex(r => r.UsuarioId);
    }
}
