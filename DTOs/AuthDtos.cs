namespace SistemaOrdemServico.DTOs;

public record LoginDto(string Usuario, string Senha);

public record LoginResponseDto(string Token, string Usuario, string? Setor, string Role);

public record CadastrarUsuarioDto(string Usuario, string Senha, string? Nome, string? Email, string Role, string? Setor);

public record UsuarioResponseDto(System.Guid Id, string Usuario, string? Setor, string Role);
