using Microsoft.EntityFrameworkCore;
using RRHH.Domain.Comun;
using RRHH.Domain.Empleados;
using RRHH.Domain.Organizacion;
using RRHH.Domain.Seguros;
using RRHH.Domain.Vacaciones;

namespace RRHH.Infrastructure.Persistencia.Semillas;

/// <summary>
/// Datos ficticios para desarrollo y demostración (solo se cargan si la base está vacía).
/// Personas, RUT y correos son inventados.
/// </summary>
internal static class DatosDemo
{
    private sealed record EmpleadoDemo(
        string Clave, int NumeroRut, string Nombres, string ApellidoPaterno, string ApellidoMaterno,
        string Departamento, string Cargo, int ComunaId, string? JefeClave,
        int AnioNacimiento, int AnioIngreso, int MesIngreso, Afp Afp, SistemaSalud Salud, int AniosPrevios);

    private static readonly (string Nombre, string Descripcion)[] Departamentos =
    [
        ("Gerencia General", "Dirección de la compañía"),
        ("Recursos Humanos", "Gestión de personas, remuneraciones y bienestar"),
        ("Tecnología", "Desarrollo de software e infraestructura"),
        ("Finanzas", "Contabilidad, tesorería y control de gestión"),
        ("Operaciones", "Logística y operaciones regionales"),
        ("Comercial", "Ventas y atención de clientes"),
    ];

    private static readonly (string Departamento, string Cargo)[] Cargos =
    [
        ("Gerencia General", "Gerente General"),
        ("Recursos Humanos", "Jefe de Recursos Humanos"),
        ("Recursos Humanos", "Analista de Remuneraciones"),
        ("Recursos Humanos", "Analista de Personas"),
        ("Tecnología", "Jefe de Tecnología"),
        ("Tecnología", "Desarrollador .NET"),
        ("Tecnología", "Desarrollador Frontend"),
        ("Finanzas", "Jefe de Finanzas"),
        ("Finanzas", "Contador"),
        ("Operaciones", "Jefe de Operaciones"),
        ("Operaciones", "Supervisor Regional"),
        ("Comercial", "Jefe Comercial"),
        ("Comercial", "Ejecutivo de Ventas"),
    ];

    private static readonly EmpleadoDemo[] Empleados =
    [
        new("gg", 9876543, "Carolina", "Fuentes", "Rojas", "Gerencia General", "Gerente General", 13114, null, 1975, 2012, 3, Afp.Habitat, SistemaSalud.Isapre, 10),
        new("rrhh", 12456789, "Marcela", "Soto", "Pizarro", "Recursos Humanos", "Jefe de Recursos Humanos", 13123, "gg", 1982, 2015, 6, Afp.Capital, SistemaSalud.Isapre, 6),
        new("rem", 17654321, "Diego", "Muñoz", "Castro", "Recursos Humanos", "Analista de Remuneraciones", 13101, "rrhh", 1991, 2019, 1, Afp.Modelo, SistemaSalud.Fonasa, 2),
        new("per", 18765432, "Valentina", "Reyes", "Díaz", "Recursos Humanos", "Analista de Personas", 5109, "rrhh", 1994, 2021, 8, Afp.Uno, SistemaSalud.Fonasa, 0),
        new("ti", 13579246, "Rodrigo", "Vergara", "Salinas", "Tecnología", "Jefe de Tecnología", 13120, "gg", 1984, 2014, 9, Afp.Cuprum, SistemaSalud.Isapre, 8),
        new("dev1", 19283746, "Camila", "Paredes", "Núñez", "Tecnología", "Desarrollador .NET", 8101, "ti", 1996, 2022, 2, Afp.Modelo, SistemaSalud.Fonasa, 1),
        new("dev2", 20123456, "Matías", "González", "Herrera", "Tecnología", "Desarrollador .NET", 9101, "ti", 1998, 2023, 5, Afp.Uno, SistemaSalud.Fonasa, 0),
        new("front", 18234567, "Javiera", "Carrasco", "Leiva", "Tecnología", "Desarrollador Frontend", 13119, "ti", 1993, 2020, 11, Afp.PlanVital, SistemaSalud.Isapre, 3),
        new("fin", 11223344, "Andrés", "Figueroa", "Morales", "Finanzas", "Jefe de Finanzas", 13114, "gg", 1979, 2011, 4, Afp.Provida, SistemaSalud.Isapre, 12),
        new("cont", 16543210, "Paula", "Espinoza", "Vidal", "Finanzas", "Contador", 6101, "fin", 1988, 2017, 7, Afp.Habitat, SistemaSalud.Fonasa, 4),
        new("ope", 14567890, "Felipe", "Araya", "Tapia", "Operaciones", "Jefe de Operaciones", 2101, "gg", 1981, 2016, 1, Afp.Capital, SistemaSalud.Isapre, 7),
        new("sup1", 17890123, "Constanza", "Olivares", "Bravo", "Operaciones", "Supervisor Regional", 4101, "ope", 1990, 2018, 10, Afp.Provida, SistemaSalud.Fonasa, 2),
        new("sup2", 16789012, "Sebastián", "Molina", "Cortés", "Operaciones", "Supervisor Regional", 10101, "ope", 1987, 2019, 3, Afp.Habitat, SistemaSalud.Fonasa, 5),
        new("sup3", 19876543, "Fernanda", "Sepúlveda", "Gallardo", "Operaciones", "Supervisor Regional", 1101, "ope", 1995, 2024, 4, Afp.Uno, SistemaSalud.Fonasa, 1),
        new("com", 15678901, "Ignacio", "Rojas", "Valenzuela", "Comercial", "Jefe Comercial", 5101, "gg", 1985, 2016, 8, Afp.Cuprum, SistemaSalud.Isapre, 6),
        new("ven1", 18901234, "Antonia", "Castillo", "Fernández", "Comercial", "Ejecutivo de Ventas", 7101, "com", 1992, 2020, 2, Afp.Modelo, SistemaSalud.Fonasa, 3),
        new("ven2", 20987654, "Benjamín", "Torres", "Sandoval", "Comercial", "Ejecutivo de Ventas", 16101, "com", 1999, 2025, 1, Afp.Uno, SistemaSalud.Fonasa, 0),
    ];

    public static async Task SembrarAsync(RrhhDbContext db, TimeProvider reloj, CancellationToken ct = default)
    {
        if (await db.Departamentos.AnyAsync(ct))
        {
            return; // La base ya tiene datos: no se toca.
        }

        var departamentos = Departamentos.ToDictionary(d => d.Nombre, d => Departamento.Crear(d.Nombre, d.Descripcion));
        db.Departamentos.AddRange(departamentos.Values);
        await db.SaveChangesAsync(ct);

        var cargos = Cargos.ToDictionary(
            c => (c.Departamento, c.Cargo),
            c => Cargo.Crear(c.Cargo, departamentos[c.Departamento].Id));
        db.Cargos.AddRange(cargos.Values);
        await db.SaveChangesAsync(ct);

        var empleados = new Dictionary<string, Empleado>();
        foreach (var e in Empleados)
        {
            var rut = Rut.Crear($"{e.NumeroRut}-{Rut.CalcularDigitoVerificador(e.NumeroRut)}");
            var email = $"{Normalizar(e.Nombres)}.{Normalizar(e.ApellidoPaterno)}@empresa-demo.cl";

            empleados[e.Clave] = Empleado.Crear(
                rut, e.Nombres, e.ApellidoPaterno, e.ApellidoMaterno, email,
                new DateOnly(e.AnioNacimiento, 1 + e.NumeroRut % 12, 1 + e.NumeroRut % 28),
                new DateOnly(e.AnioIngreso, e.MesIngreso, 1),
                departamentos[e.Departamento].Id,
                cargos[(e.Departamento, e.Cargo)].Id,
                e.ComunaId,
                e.Afp,
                e.Salud,
                e.AniosPrevios);
        }

        db.Empleados.AddRange(empleados.Values);
        await db.SaveChangesAsync(ct);

        foreach (var e in Empleados.Where(e => e.JefeClave is not null))
        {
            empleados[e.Clave].AsignarJefe(empleados[e.JefeClave!].Id);
        }

        await db.SaveChangesAsync(ct);

        // Seguros complementarios
        var planes = new[]
        {
            PlanSeguro.Crear("Salud Complementario Plus", "Seguros Andes", TipoSeguro.Salud, 0.85m),
            PlanSeguro.Crear("Vida Colectivo", "Seguros Andes", TipoSeguro.Vida, 0.35m),
            PlanSeguro.Crear("Dental Familiar", "Aseguradora Pacífico", TipoSeguro.Dental, 0.28m),
            PlanSeguro.Crear("Catastrófico Integral", "Aseguradora Pacífico", TipoSeguro.Catastrofico, 0.42m),
        };
        db.PlanesSeguro.AddRange(planes);
        await db.SaveChangesAsync(ct);

        var numeroCargas = 0;
        foreach (var empleado in empleados.Values)
        {
            db.AfiliacionesSeguro.Add(AfiliacionSeguro.Crear(empleado.Id, planes[0].Id, empleado.FechaIngreso, numeroCargas++ % 4));
            db.AfiliacionesSeguro.Add(AfiliacionSeguro.Crear(empleado.Id, planes[1].Id, empleado.FechaIngreso, 0));
        }

        // Vacaciones: una aprobada en el pasado y dos pendientes para que el jefe de TI las revise.
        var ahora = reloj.GetUtcNow();
        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
        var feriados = (await db.Feriados.Select(f => f.Fecha).ToListAsync(ct)).ToHashSet();

        var inicioPasado = ProximoLunes(hoy.AddDays(-60));
        var aprobada = CrearSolicitud(empleados["dev1"].Id, inicioPasado, inicioPasado.AddDays(4), feriados, ahora.AddDays(-75));
        aprobada.Aprobar(empleados["ti"].Id, ahora.AddDays(-70));

        var inicioFuturo = ProximoLunes(hoy.AddDays(30));
        db.SolicitudesVacaciones.AddRange(
            aprobada,
            CrearSolicitud(empleados["dev2"].Id, inicioFuturo, inicioFuturo.AddDays(4), feriados, ahora),
            CrearSolicitud(empleados["front"].Id, inicioFuturo.AddDays(14), inicioFuturo.AddDays(18), feriados, ahora));

        await db.SaveChangesAsync(ct);
    }

    private static SolicitudVacaciones CrearSolicitud(
        int empleadoId, DateOnly inicio, DateOnly fin, IReadOnlySet<DateOnly> feriados, DateTimeOffset fecha) =>
        SolicitudVacaciones.Crear(
            empleadoId, inicio, fin, CalculadoraVacaciones.ContarDiasHabiles(inicio, fin, feriados), "Datos de demostración", fecha);

    private static DateOnly ProximoLunes(DateOnly fecha)
    {
        while (fecha.DayOfWeek != DayOfWeek.Monday)
        {
            fecha = fecha.AddDays(1);
        }

        return fecha;
    }

    private static string Normalizar(string texto) =>
        new string(texto.ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => char.IsAsciiLetter(c))
            .ToArray());
}
