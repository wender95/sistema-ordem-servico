using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaOrdemServico.Domain.Entities;
using SistemaOrdemServico.Domain.Enums;
using SistemaOrdemServico.Domain.Interfaces;
using SistemaOrdemServico.DTOs;
using SistemaOrdemServico.Infrastructure.Security;

namespace SistemaOrdemServico.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(IUsuarioRepository usuarioRepository, JwtTokenService jwtTokenService)
    {
        _usuarioRepository = usuarioRepository;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Usuario) || string.IsNullOrWhiteSpace(model.Senha))
            return BadRequest("Informe usuário e senha.");

        var usuario = await _usuarioRepository.ObterPorNomeUsuarioAsync(model.Usuario.Trim());

        // Mesma mensagem para "usuário não existe" e "senha errada" — não dá pista de
        // quais usernames existem no sistema (evita user enumeration).
        if (usuario == null || !PasswordHasher.Verificar(model.Senha.Trim(), usuario.SenhaHash, usuario.SenhaSalt))
            return Unauthorized("Usuário ou senha incorretos.");

        var token = _jwtTokenService.GerarToken(usuario);

        return Ok(new LoginResponseDto(
            token,
            usuario.NomeUsuario,
            usuario.Setor?.ToString(),
            usuario.Perfil.ToString()));
    }

    [HttpGet("usuarios")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ListarUsuarios()
    {
        var usuarios = await _usuarioRepository.ObterTodosAsync();

        var lista = usuarios.Select(u => new UsuarioResponseDto(
            u.Id,
            u.NomeUsuario,
            u.Setor?.ToString(),
            u.Perfil.ToString()));

        return Ok(lista);
    }

    [HttpPost("usuarios")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CadastrarUsuario([FromBody] CadastrarUsuarioDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Usuario))
            return BadRequest("Nome de usuário é obrigatório.");

        if (await _usuarioRepository.ExisteComNomeUsuarioAsync(dto.Usuario.Trim()))
            return Conflict("Usuário já cadastrado.");

        if (!Enum.TryParse<TipoPerfil>(dto.Role, ignoreCase: true, out var perfil))
            return BadRequest($"Role inválida. Valores aceitos: {string.Join(", ", Enum.GetNames<TipoPerfil>())}");

        SetorEnum? setor = null;
        if (!string.IsNullOrWhiteSpace(dto.Setor))
        {
            if (!Enum.TryParse<SetorEnum>(dto.Setor, ignoreCase: true, out var setorParsed))
                return BadRequest($"Setor inválido. Valores aceitos: {string.Join(", ", Enum.GetNames<SetorEnum>())}");
            setor = setorParsed;
        }

        if (perfil == TipoPerfil.Operacional && !setor.HasValue)
            return BadRequest("Usuários operacionais precisam de um setor.");

        if (perfil == TipoPerfil.Vendedor && setor.HasValue && setor != SetorEnum.Vendas)
            return BadRequest("Vendedores devem pertencer ao setor Vendas ou ficar sem setor específico.");

        var senhaTemporaria = string.IsNullOrWhiteSpace(dto.Senha) ? Guid.NewGuid().ToString("N")[..10] : dto.Senha;

        if (senhaTemporaria.Length < 8)
            return BadRequest("A senha deve possuir pelo menos 8 caracteres.");

        var usuario = Usuario.Criar(
            dto.Usuario.Trim(),
            senhaTemporaria,
            perfil,
            setor,
            PasswordHasher.Hash,
            dto.Nome,
            dto.Email);

        await _usuarioRepository.AdicionarAsync(usuario);

        // Retornamos a senha temporária só nesta resposta (nunca fica salva em texto puro no banco).
        return Ok(new { usuario.Id, usuario.NomeUsuario, SenhaTemporaria = senhaTemporaria });
    }
}
