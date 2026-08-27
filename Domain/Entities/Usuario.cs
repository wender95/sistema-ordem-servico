using System;
using SistemaOrdemServico.Domain.Enums;

namespace SistemaOrdemServico.Domain.Entities;

public class Usuario
{
    public Guid Id { get; private set; }
    public string NomeUsuario { get; private set; } = string.Empty; // login (ex: "admin")
    public string Nome { get; private set; } = string.Empty;        // nome de exibição
    public string? Email { get; private set; }
    public string SenhaHash { get; private set; } = string.Empty;
    public string SenhaSalt { get; private set; } = string.Empty;
    public TipoPerfil Perfil { get; private set; }
    public SetorEnum? Setor { get; private set; }

    private Usuario() { } // EF Core

    private Usuario(string nomeUsuario, string nome, string? email, TipoPerfil perfil, SetorEnum? setor)
    {
        Id = Guid.NewGuid();
        NomeUsuario = nomeUsuario.Trim();
        Nome = string.IsNullOrWhiteSpace(nome) ? nomeUsuario.Trim() : nome.Trim();
        Email = email;
        Perfil = perfil;
        Setor = setor;
    }

    /// <summary>
    /// Cria um novo usuário já com a senha em texto puro sendo transformada em hash.
    /// Nenhuma senha em texto puro é armazenada.
    /// </summary>
    public static Usuario Criar(
        string nomeUsuario,
        string senha,
        TipoPerfil perfil,
        SetorEnum? setor,
        Func<string, (string hash, string salt)> hasher,
        string? nome = null,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(nomeUsuario))
            throw new ArgumentException("Nome de usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException("Senha é obrigatória.");

        var usuario = new Usuario(nomeUsuario, nome ?? nomeUsuario, email, perfil, setor);
        var (hash, salt) = hasher(senha);
        usuario.SenhaHash = hash;
        usuario.SenhaSalt = salt;
        return usuario;
    }

    public void DefinirSenha(string senha, Func<string, (string hash, string salt)> hasher)
    {
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException("Senha é obrigatória.");

        var (hash, salt) = hasher(senha);
        SenhaHash = hash;
        SenhaSalt = salt;
    }
}
