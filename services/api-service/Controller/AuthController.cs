using System.Runtime.CompilerServices;
using api_service.Data;
using api_service.Models;
using api_service.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace api_service.Controller;
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtservice;
    public AuthController(AppDbContext context, JwtService jwtService)
    {
        _context = context;
        _jwtservice = jwtService;
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if(existingUser != null)
        {
            return BadRequest("Email đã tồn tại");
        }
        var user = new Users
        {
            Username =request.Username,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreateAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok("Đăng ký thành công");
    }
    [HttpPost("login")]
    public async Task<IActionResult>Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if(user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return Unauthorized("Email hoặc mật khẩu không đúng");
        }
        var token = _jwtservice.GenerateToken(user.Username);
        return Ok(new 
        {
            access_token = token,
            messager = "Đăng nhập thành công"  
        });
    }
}