using api_service.Models;
using api_service.Models.Common;

namespace api_service.Interface;

public interface IRefreshService
{
    Task<Token> GenerateTokenAsync(Users user);
    Task<Result<Token>> RefreshAsync(string refreshToken);
    Task<Result<string>> RevokeAsync(string refreshToken);
}
