namespace api_service.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string PrivateKeyPem { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
    public string PrivateKeyBase64 { get; set; } = string.Empty;
    public string PublicKeyBase64 { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int Expireminutes { get; set; }
    public int RefreshTokenExpireDays { get; set; }
}
