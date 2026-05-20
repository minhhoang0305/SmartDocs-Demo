using api_service.Models;

namespace api_service.Interface;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterRequest request);
    Task<Users?> LoginAsync(LoginRequest request);
}