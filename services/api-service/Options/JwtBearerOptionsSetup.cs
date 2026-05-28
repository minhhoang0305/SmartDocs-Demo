using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace api_service.Options;

public class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions _jwtOptions;

    public JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public void Configure(JwtBearerOptions options)
    {
        Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        var key = JwtRsaKeyReader.CreatePublicKey(_jwtOptions);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = key,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        };
    }
}
