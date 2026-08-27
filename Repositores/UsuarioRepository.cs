using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaOrdemServico.Data;
using SistemaOrdemServico.Domain.Entities;
using SistemaOrdemServico.Domain.Interfaces;

namespace SistemaOrdemServico.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Usuario usuario)
    {
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();
    }

    public async Task<Usuario?> ObterPorNomeUsuarioAsync(string nomeUsuario)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.NomeUsuario.ToLower() == nomeUsuario.ToLower());
    }

    public async Task<IEnumerable<Usuario>> ObterTodosAsync()
    {
        return await _context.Usuarios.AsNoTracking().ToListAsync();
    }

    public async Task<bool> ExisteComNomeUsuarioAsync(string nomeUsuario)
    {
        return await _context.Usuarios
            .AnyAsync(u => u.NomeUsuario.ToLower() == nomeUsuario.ToLower());
    }
}
