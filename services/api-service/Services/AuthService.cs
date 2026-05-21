using api_service.Models;
using api_service.Data;
using api_service.Interface;
using Microsoft.EntityFrameworkCore;


namespace api_service.Services;
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    public AuthService(AppDbContext context)
    {
        _context = context;
    }
    public async Task<string> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if(existingUser != null)
        {
            return "Email đã tồn tại";
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
        return "Đăng ký thành công";
    }
    public async Task<Users?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if(user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return null;
        }
        return user;
    }
}
