using api_service.Models;
using api_service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace api_service.Controller;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly IAuthService _authService;

    public AuthController(IJwtService jwtService, IAuthService authService)
    {
        _jwtService = jwtService;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (result != "Đăng ký thành công")
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _authService.LoginAsync(request);

        if (user == null)
            return Unauthorized("Email hoặc mật khẩu không đúng");

        var token = _jwtService.GenerateToken(user.Username, user.Email);

        return Ok(new
        {
            access_token = token,
            messager = "Đăng nhập thành công"
        });
    }
}