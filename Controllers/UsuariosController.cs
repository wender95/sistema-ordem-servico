using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaOrdemServico.Data;
using SistemaOrdemServico.Domain.Entities;
using SistemaOrdemServico.Domain.Enums;
using SistemaOrdemServico.Domain.Interfaces;
using SistemaOrdemServico.Infrastructure.Security;

namespace SistemaOrdemServico.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")] // Apenas administradores podem gerenciar usuários
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuariosController(AppDbContext context, IUsuarioRepository usuarioRepository)
    {
        _context = context;
        _usuarioRepository = usuarioRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> ObterTodos()
    {
        var usuarios = await _context.Usuarios
            .Select(u => new
            {
                u.Id,
                u.NomeUsuario,
                u.Nome,
                u.Email,
                Perfil = u.Perfil.ToString(),
                Setor = u.Setor.HasValue ? u.Setor.ToString() : null
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioDto dto)
    {
        if (dto is null)
            return BadRequest("Dados inválidos.");

        if (string.IsNullOrWhiteSpace(dto.NomeUsuario) || string.IsNullOrWhiteSpace(dto.Senha))
            return BadRequest("Nome de usuário e senha são obrigatórios.");

        // Verifica se já existe um usuário com esse nome
        var usuarioExistente = await _usuarioRepository.ObterPorNomeUsuarioAsync(dto.NomeUsuario);
        if (usuarioExistente is not null)
            return BadRequest("Já existe um usuário cadastrado com este nome de usuário.");

        var novoUsuario = Usuario.Criar(
            nomeUsuario: dto.NomeUsuario,
            senha: dto.Senha,
            perfil: dto.Perfil,
            setor: dto.Setor,
            hasher: PasswordHasher.Hash,
            nome: dto.Nome ?? dto.NomeUsuario,
            email: dto.Email);

        _context.Usuarios.Add(novoUsuario);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Usuário criado com sucesso!",
            id = novoUsuario.Id,
            novoUsuario.NomeUsuario,
            novoUsuario.Nome,
            perfil = novoUsuario.Perfil.ToString()
        });
    }
}

// DTO interno para recebimento de dados de cadastro de usuário
public class CriarUsuarioDto
{
    public string NomeUsuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public TipoPerfil Perfil { get; set; }
    public SetorEnum? Setor { get; set; }
}