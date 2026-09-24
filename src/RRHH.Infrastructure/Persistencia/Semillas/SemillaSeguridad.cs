using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RRHH.Application.Seguridad;
using RRHH.Domain.Seguridad;

namespace RRHH.Infrastructure.Persistencia.Semillas;

/// <summary>Crea el primer administrador y, en desarrollo, usuarios de demostración.</summary>
internal static class SemillaSeguridad
{
    /// <summary>
    /// Si no existe ningún usuario, crea el Admin inicial con "Seguridad:AdminInicial:Email" y ":Clave"
    /// (definidos en user-secrets o variables de entorno; nunca en el repositorio).
    /// </summary>
    public static async Task CrearAdminInicialAsync(
        RrhhDbContext db, IHasherClaves hasher, IConfiguration configuracion, TimeProvider reloj, ILogger logger, CancellationToken ct)
    {
        var email = configuracion["Seguridad:AdminInicial:Email"];
        var clave = configuracion["Seguridad:AdminInicial:Clave"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(clave))
        {
            return; // sin configuración no se crea nada (ni se consulta la base)
        }

        if (await db.Usuarios.AnyAsync(u => u.Rol == Rol.Admin, ct))
        {
            return;
        }

        PoliticaClaves.Validar(clave, email);
        db.Usuarios.Add(Usuario.Crear(email, hasher.Hashear(clave), Rol.Admin, null, null, reloj.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Administrador inicial creado: {Email}", email);
    }

    /// <summary>
    /// Usuarios para los empleados demo (solo Development). Muestran cada rol y alcance:
    /// Admin, RRHH nacional, RRHH de una región, jefaturas y empleados.
    /// </summary>
    public static async Task CrearUsuariosDemoAsync(
        RrhhDbContext db, IHasherClaves hasher, string clave, TimeProvider reloj, CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(u => u.EmpleadoId != null, ct))
        {
            return;
        }

        var todas = Enumerable.Range(1, 16).ToArray();
        var asignaciones = new (string Email, Rol Rol, int[]? Regiones)[]
        {
            ("carolina.fuentes@empresa-demo.cl", Rol.Admin, null),
            ("marcela.soto@empresa-demo.cl", Rol.RRHH, todas),
            ("diego.munoz@empresa-demo.cl", Rol.RRHH, [13]),       // solo Región Metropolitana
            ("valentina.reyes@empresa-demo.cl", Rol.RRHH, [5]),    // solo Valparaíso
            ("rodrigo.vergara@empresa-demo.cl", Rol.Jefatura, null),
            ("andres.figueroa@empresa-demo.cl", Rol.Jefatura, null),
            ("felipe.araya@empresa-demo.cl", Rol.Jefatura, null),
            ("ignacio.rojas@empresa-demo.cl", Rol.Jefatura, null),
        };

        var hash = hasher.Hashear(clave);
        var ahora = reloj.GetUtcNow();
        var empleados = await db.Empleados.Where(e => e.FechaTermino == null).ToListAsync(ct);

        foreach (var empleado in empleados)
        {
            var asignacion = asignaciones.FirstOrDefault(a => a.Email == empleado.Email);
            var rol = asignacion.Email is null ? Rol.Empleado : asignacion.Rol;

            db.Usuarios.Add(Usuario.Crear(empleado.Email, hash, rol, empleado.Id, asignacion.Regiones, ahora));
        }

        await db.SaveChangesAsync(ct);
    }
}
