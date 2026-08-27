using System;
using System.Security.Cryptography;

namespace SistemaOrdemServico.Infrastructure.Security;

/// <summary>
/// Hash de senha com PBKDF2 (Rfc2898DeriveBytes), usando apenas o que já vem no .NET —
/// sem depender de mais um pacote NuGet além do BCL.
/// </summary>
public static class PasswordHasher
{
    private const int TamanhoSalt = 16;      // bytes
    private const int TamanhoHash = 32;      // bytes
    private const int Iteracoes = 100_000;
    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public static (string hash, string salt) Hash(string senha)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(senha, saltBytes, Iteracoes, Algoritmo, TamanhoHash);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public static bool Verificar(string senha, string hashArmazenado, string saltArmazenado)
    {
        var saltBytes = Convert.FromBase64String(saltArmazenado);
        var hashEsperado = Convert.FromBase64String(hashArmazenado);

        var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(senha, saltBytes, Iteracoes, Algoritmo, TamanhoHash);

        // Comparação em tempo constante para evitar timing attacks
        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }
}
