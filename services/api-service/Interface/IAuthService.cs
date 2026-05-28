using api_service.Models;
using api_service.Models.Common;

namespace api_service.Interface;

public interface IAuthService
{
    Task<Result<string>> RegisterAsync(RegisterRequest request);
    Task<Result<Users>> LoginAsync(LoginRequest request);
}
