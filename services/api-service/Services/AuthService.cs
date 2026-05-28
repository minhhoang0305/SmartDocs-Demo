using api_service.Models;
using api_service.Data;
using api_service.Interface;
using api_service.Models.Common;
using Microsoft.EntityFrameworkCore;


namespace api_service.Services;
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    public AuthService(AppDbContext context)
    {
        _context = context;
    }
    public async Task<Result<string>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if(existingUser != null)
        {
            return Result<string>.Failure(
                new Error("Auth.EmailAlreadyExists", "Email đã tồn tại"));
        }

        var user = new Users
        {
            Username = request.Username,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreateAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Result<string>.Success("Đăng ký thành công");
    }

    public async Task<Result<Users>> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if(user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return Result<Users>.Failure(
                new Error("Auth.InvalidCredentials", "Email hoặc mật khẩu không đúng"));
        }

        return Result<Users>.Success(user);
    }
}
