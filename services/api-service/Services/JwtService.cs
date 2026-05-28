using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using api_service.Interface;
using api_service.Options;
using Microsoft.Extensions.Options;

namespace api_service.Services;
public class JwtService : IJwtService
{
    private readonly JwtOptions _jwtOptions;
    public JwtService(IOptions<JwtOptions> jwtOptions)
    {   
        _jwtOptions = jwtOptions.Value;
    }
    public string GenerateToken(string Username, string Email, string Role)
    {
        var securityKey = JwtRsaKeyReader.CreatePrivateKey(_jwtOptions);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
        var claim = new[]
        {
            new Claim(ClaimTypes.Name, Username),
            new Claim(ClaimTypes.Email, Email),
            new Claim(ClaimTypes.Role, Role)
        };
        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claim,
            expires:
            DateTime.UtcNow.AddMinutes(_jwtOptions.Expireminutes),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
