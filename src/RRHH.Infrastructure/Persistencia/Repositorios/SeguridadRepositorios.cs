using Microsoft.EntityFrameworkCore;
using RRHH.Application.Seguridad;
using RRHH.Domain.Seguridad;

namespace RRHH.Infrastructure.Persistencia.Repositorios;

internal sealed class UsuarioRepositorio(RrhhDbContext db) : IUsuarioRepositorio
{
    public Task<Usuario?> ObtenerAsync(int id, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObtenerPorEmailAsync(string emailNormalizado, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNormalizado, ct);

    public Task<bool> ExisteEmailAsync(string emailNormalizado, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.Email == emailNormalizado, ct);

    public Task<bool> ExisteParaEmpleadoAsync(int empleadoId, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.EmpleadoId == empleadoId, ct);

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => db.Usuarios.AnyAsync(ct);

    public void Agregar(Usuario usuario) => db.Usuarios.Add(usuario);
}

internal sealed class TokenRenovacionRepositorio(RrhhDbContext db) : ITokenRenovacionRepositorio
{
    public Task<TokenRenovacion?> ObtenerPorHashAsync(string hash, CancellationToken ct) =>
        db.TokensRenovacion.FirstOrDefaultAsync(t => t.Hash == hash, ct);

    public async Task RevocarTodosAsync(int usuarioId, DateTimeOffset ahora, string motivo, CancellationToken ct)
    {
        var vigentes = await db.TokensRenovacion
            .Where(t => t.UsuarioId == usuarioId && t.RevocadoEn == null)
            .ToListAsync(ct);

        foreach (var token in vigentes)
        {
            token.Revocar(ahora, motivo);
        }
    }

    public void Agregar(TokenRenovacion token) => db.TokensRenovacion.Add(token);
}
