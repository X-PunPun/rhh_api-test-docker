using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRHH.Domain.Organizacion;

namespace RRHH.Infrastructure.Persistencia.Configuraciones;

internal sealed class DepartamentoConfiguracion : IEntityTypeConfiguration<Departamento>
{
    public void Configure(EntityTypeBuilder<Departamento> builder)
    {
        builder.ToTable("Departamentos");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Nombre).HasMaxLength(Departamento.LargoMaximoNombre).IsRequired();
        builder.Property(d => d.Descripcion).HasMaxLength(Departamento.LargoMaximoDescripcion);
        builder.HasIndex(d => d.Nombre).IsUnique();
    }
}
