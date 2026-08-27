namespace SistemaOrdemServico.Infrastructure.Security;

/// <summary>
/// Mapeia a seção "Jwt" do appsettings.json / variáveis de ambiente.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 480; // 8h, ajuste conforme a necessidade
}
