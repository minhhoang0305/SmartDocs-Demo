using api_service.Models;
using api_service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace api_service.Controller;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRefreshService _refreshService;

    public AuthController(
        IAuthService authService,
        IRefreshService refreshService)
    {
        _authService = authService;
        _refreshService = refreshService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result.IsFailure)
            return Unauthorized(result.Error);

        var user = result.Value!;
        var token = await _refreshService.GenerateTokenAsync(user);

        return Ok(new
        {
            access_token = token.AccessToken,
            refresh_token = token.RefreshToken,
            messager = "Đăng nhập thành công"
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        var result = await _refreshService.RefreshAsync(request.RefreshToken);

        if (result.IsFailure)
            return Unauthorized(result.Error);

        var token = result.Value!;

        return Ok(new
        {
            access_token = token.AccessToken,
            refresh_token = token.RefreshToken
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        var result = await _refreshService.RevokeAsync(request.RefreshToken);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
