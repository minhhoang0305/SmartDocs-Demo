using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using api_service.Interface;

namespace api_service.Services;
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    public JwtService(IConfiguration configuration)
    {   
        _configuration = configuration;
    }
    public string GenerateToken(string Username, string Email, string Role)
    {
        var key = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expireminutes = int.Parse(_configuration["Jwt:Expireminutes"]!);
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claim = new[]
        {
            new Claim(ClaimTypes.Name, Username),
            new Claim(ClaimTypes.Email, Email),
            new Claim(ClaimTypes.Role, Role)
        };
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claim,
            expires:
            DateTime.UtcNow.AddMinutes(expireminutes),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}