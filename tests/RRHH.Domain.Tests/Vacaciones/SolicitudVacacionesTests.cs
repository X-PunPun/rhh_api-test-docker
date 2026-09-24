using RRHH.Domain.Comun;
using RRHH.Domain.Vacaciones;

namespace RRHH.Domain.Tests.Vacaciones;

public class SolicitudVacacionesTests
{
    private const int EmpleadoId = 10;
    private const int JefeId = 5;
    private static readonly DateTimeOffset Ahora = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private static SolicitudVacaciones Nueva() =>
        SolicitudVacaciones.Crear(EmpleadoId, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6), 5, "Viaje", Ahora);

    [Fact]
    public void Crear_QuedaPendiente()
    {
        Assert.Equal(EstadoSolicitud.Pendiente, Nueva().Estado);
    }

    [Fact]
    public void Crear_FinAntesDeInicio_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() =>
            SolicitudVacaciones.Crear(EmpleadoId, new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 2), 5, null, Ahora));
    }

    [Fact]
    public void Crear_SinDiasHabiles_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() =>
            SolicitudVacaciones.Crear(EmpleadoId, new DateOnly(2026, 11, 7), new DateOnly(2026, 11, 8), 0, null, Ahora));
    }

    [Fact]
    public void Aprobar_PorJefe_CambiaEstadoYRegistraAprobador()
    {
        var solicitud = Nueva();

        solicitud.Aprobar(JefeId, Ahora);

        Assert.Equal(EstadoSolicitud.Aprobada, solicitud.Estado);
        Assert.Equal(JefeId, solicitud.ResueltaPorId);
    }

    [Fact]
    public void Aprobar_PorElMismoEmpleado_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => Nueva().Aprobar(EmpleadoId, Ahora));
    }

    [Fact]
    public void Aprobar_DosVeces_LanzaExcepcion()
    {
        var solicitud = Nueva();
        solicitud.Aprobar(JefeId, Ahora);

        Assert.Throws<ExcepcionDominio>(() => solicitud.Aprobar(JefeId, Ahora));
    }

    [Fact]
    public void Rechazar_SinMotivo_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => Nueva().Rechazar(JefeId, " ", Ahora));
    }

    [Fact]
    public void Cancelar_PorOtroEmpleado_LanzaExcepcion()
    {
        Assert.Throws<ExcepcionDominio>(() => Nueva().Cancelar(JefeId, Ahora));
    }

    [Fact]
    public void Cancelar_Aprobada_LanzaExcepcion()
    {
        var solicitud = Nueva();
        solicitud.Aprobar(JefeId, Ahora);

        Assert.Throws<ExcepcionDominio>(() => solicitud.Cancelar(EmpleadoId, Ahora));
    }

    [Theory]
    [InlineData("2026-10-26", "2026-11-02", true)]   // termina el día que empieza
    [InlineData("2026-11-06", "2026-11-10", true)]   // empieza el día que termina
    [InlineData("2026-11-03", "2026-11-04", true)]   // contenida
    [InlineData("2026-10-26", "2026-10-30", false)]
    [InlineData("2026-11-09", "2026-11-13", false)]
    public void SeSuperponeCon_DetectaCruces(string inicio, string fin, bool esperado)
    {
        Assert.Equal(esperado, Nueva().SeSuperponeCon(DateOnly.Parse(inicio), DateOnly.Parse(fin)));
    }
}
