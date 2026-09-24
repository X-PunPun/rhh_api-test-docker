using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RRHH.Application.Comun;

namespace RRHH.Infrastructure.Persistencia;

/// <summary>Confirma los cambios y traduce errores de base de datos a excepciones de aplicación.</summary>
internal sealed class UnidadDeTrabajo(RrhhDbContext db) : IUnidadDeTrabajo
{
    // Códigos de SQL Server para violación de índice único / clave primaria.
    private const int ErrorIndiceUnico = 2601;
    private const int ErrorClaveUnica = 2627;

    public async Task GuardarCambiosAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoException("El registro fue modificado por otro usuario. Recargue e intente nuevamente.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: ErrorIndiceUnico or ErrorClaveUnica })
        {
            throw new ConflictoException("Ya existe un registro con esos datos.");
        }
    }
}
