using RRHH.Application.Comun;

namespace RRHH.Application.Tests.Comun;

public class RelojChileTests
{
    [Theory]
    // Septiembre (horario de verano, UTC-3): 02:30 UTC del 25 sigue siendo el 24 en Chile.
    [InlineData("2026-09-25T02:30:00Z", "2026-09-24")]
    // Junio (horario de invierno, UTC-4): 03:59 UTC del 10 sigue siendo el 9 en Chile.
    [InlineData("2026-06-10T03:59:00Z", "2026-06-09")]
    [InlineData("2026-06-10T04:00:00Z", "2026-06-10")]
    public void Hoy_usa_la_fecha_de_Chile_y_no_la_del_servidor(string utc, string esperado)
    {
        var reloj = new RelojFijoChile(DateTimeOffset.Parse(utc));

        Assert.Equal(DateOnly.Parse(esperado), reloj.Hoy());
    }

    /// <summary>Mismo huso que <see cref="RelojChile"/>, pero con la hora UTC fija.</summary>
    private sealed class RelojFijoChile(DateTimeOffset utc) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone { get; } = new RelojChile().LocalTimeZone;
        public override DateTimeOffset GetUtcNow() => utc;
    }
}
