using Microsoft.EntityFrameworkCore;
using RRHH.Domain.Empleados;
using RRHH.Domain.Organizacion;
using RRHH.Domain.Ubicacion;

namespace RRHH.Infrastructure.Persistencia;

public sealed class RrhhDbContext(DbContextOptions<RrhhDbContext> options) : DbContext(options)
{
    public const string Esquema = "rrhh";

    public DbSet<Region> Regiones => Set<Region>();
    public DbSet<Comuna> Comunas => Set<Comuna>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Empleado> Empleados => Set<Empleado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);
        // Aplica todas las clases IEntityTypeConfiguration<T> de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RrhhDbContext).Assembly);
    }
}
