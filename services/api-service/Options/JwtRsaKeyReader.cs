using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace api_service.Options;

public static class JwtRsaKeyReader
{
    public static RsaSecurityKey CreatePrivateKey(JwtOptions options)
    {
        var rsa = RSA.Create();
        ImportPrivateKey(rsa, options);
        return new RsaSecurityKey(rsa);
    }

    public static RsaSecurityKey CreatePublicKey(JwtOptions options)
    {
        var rsa = RSA.Create();
        ImportPublicKey(rsa, options);
        return new RsaSecurityKey(rsa);
    }

    public static bool HasPrivateKey(JwtOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.PrivateKeyPem)
            || !string.IsNullOrWhiteSpace(options.PrivateKeyBase64);
    }

    public static bool HasPublicKey(JwtOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.PublicKeyPem)
            || !string.IsNullOrWhiteSpace(options.PublicKeyBase64);
    }

    public static bool CanCreatePrivateKey(JwtOptions options)
    {
        return CanImport(rsa => ImportPrivateKey(rsa, options));
    }

    public static bool CanCreatePublicKey(JwtOptions options)
    {
        return CanImport(rsa => ImportPublicKey(rsa, options));
    }

    private static void ImportPrivateKey(RSA rsa, JwtOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PrivateKeyPem))
        {
            rsa.ImportFromPem(NormalizePem(options.PrivateKeyPem));
            return;
        }

        var keyBytes = GetBase64KeyBytes(options.PrivateKeyBase64, "Jwt private key");
        if (TryImportPem(rsa, keyBytes))
            return;

        if (TryImportPkcs8PrivateKey(rsa, keyBytes))
            return;

        if (TryImportRsaPrivateKey(rsa, keyBytes))
            return;

        throw new InvalidOperationException(
            "Jwt private key must be a PEM key, base64 encoded PEM, PKCS#8 DER, or RSA private key DER.");
    }

    private static void ImportPublicKey(RSA rsa, JwtOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PublicKeyPem))
        {
            rsa.ImportFromPem(NormalizePem(options.PublicKeyPem));
            return;
        }

        var keyBytes = GetBase64KeyBytes(options.PublicKeyBase64, "Jwt public key");
        if (TryImportPem(rsa, keyBytes))
            return;

        if (TryImportSubjectPublicKeyInfo(rsa, keyBytes))
            return;

        if (TryImportRsaPublicKey(rsa, keyBytes))
            return;

        throw new InvalidOperationException(
            "Jwt public key must be a PEM key, base64 encoded PEM, SubjectPublicKeyInfo DER, or RSA public key DER.");
    }

    private static bool CanImport(Action<RSA> import)
    {
        try
        {
            using var rsa = RSA.Create();
            import(rsa);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] GetBase64KeyBytes(string base64Key, string keyName)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new InvalidOperationException($"{keyName} is required");

        try
        {
            return Convert.FromBase64String(base64Key);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"{keyName} base64 value is invalid.", ex);
        }
    }

    private static bool TryImportPem(RSA rsa, byte[] keyBytes)
    {
        var keyText = Encoding.UTF8.GetString(keyBytes);
        if (!keyText.Contains("-----BEGIN", StringComparison.Ordinal))
            return false;

        try
        {
            rsa.ImportFromPem(NormalizePem(keyText));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryImportPkcs8PrivateKey(RSA rsa, byte[] keyBytes)
    {
        try
        {
            rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool TryImportRsaPrivateKey(RSA rsa, byte[] keyBytes)
    {
        try
        {
            rsa.ImportRSAPrivateKey(keyBytes, out _);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool TryImportSubjectPublicKeyInfo(RSA rsa, byte[] keyBytes)
    {
        try
        {
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool TryImportRsaPublicKey(RSA rsa, byte[] keyBytes)
    {
        try
        {
            rsa.ImportRSAPublicKey(keyBytes, out _);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string NormalizePem(string pem)
    {
        return pem.Replace("\\n", "\n");
    }
}
