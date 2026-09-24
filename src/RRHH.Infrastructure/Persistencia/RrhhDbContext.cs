using Microsoft.EntityFrameworkCore;
using RRHH.Domain.Calendario;
using RRHH.Domain.Empleados;
using RRHH.Domain.Organizacion;
using RRHH.Domain.Seguridad;
using RRHH.Domain.Seguros;
using RRHH.Domain.Ubicacion;
using RRHH.Domain.Vacaciones;

namespace RRHH.Infrastructure.Persistencia;

public sealed class RrhhDbContext(DbContextOptions<RrhhDbContext> options) : DbContext(options)
{
    public const string Esquema = "rrhh";

    public DbSet<Region> Regiones => Set<Region>();
    public DbSet<Comuna> Comunas => Set<Comuna>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Feriado> Feriados => Set<Feriado>();
    public DbSet<SolicitudVacaciones> SolicitudesVacaciones => Set<SolicitudVacaciones>();
    public DbSet<PlanSeguro> PlanesSeguro => Set<PlanSeguro>();
    public DbSet<AfiliacionSeguro> AfiliacionesSeguro => Set<AfiliacionSeguro>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenRenovacion> TokensRenovacion => Set<TokenRenovacion>();
    public DbSet<RegistroAuditoria> Auditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);
        // Aplica todas las clases IEntityTypeConfiguration<T> de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RrhhDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Los enums se guardan como texto legible ("Aprobada", "Fonasa") en vez de números.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
    }
}
