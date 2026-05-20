namespace api_service.Interface;
public interface IJwtService
{
    string GenerateToken(string username, string email);
}