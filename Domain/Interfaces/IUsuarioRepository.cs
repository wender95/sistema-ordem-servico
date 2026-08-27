using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaOrdemServico.Domain.Entities;

namespace SistemaOrdemServico.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task AdicionarAsync(Usuario usuario);
    Task<Usuario?> ObterPorNomeUsuarioAsync(string nomeUsuario);
    Task<IEnumerable<Usuario>> ObterTodosAsync();
    Task<bool> ExisteComNomeUsuarioAsync(string nomeUsuario);
}
