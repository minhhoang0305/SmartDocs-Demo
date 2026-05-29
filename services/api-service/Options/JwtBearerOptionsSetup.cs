using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace api_service.Options;

public class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions _jwtOptions;
    private readonly RsaSecurityKey _rsaPublicKey;

    public JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions, RsaSecurityKey rsaPublicKey)
    {
        _jwtOptions = jwtOptions.Value;
        _rsaPublicKey = rsaPublicKey;
    }

    public void Configure(JwtBearerOptions options)
    {
        Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = _rsaPublicKey,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        };
    }
}
