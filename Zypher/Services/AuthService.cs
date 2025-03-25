using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Model;
using Model.Model;

public class AuthService
{
    private readonly ContextClass _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(ContextClass context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !VerifyPassword(password, user.Password))
            return false;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = true };

        await _httpContextAccessor.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return true;
    }

    public async Task LogoutAsync()
    {
        await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
    
    public async Task<bool> RegisterAsync(string username, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
            return false; 

        var newUser = new User
        {
            Username = username,
            Password = HashPassword(password)
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return true;
    }

    private static bool VerifyPassword(string enteredPassword, string storedHash)
    {
        return HashPassword(enteredPassword) == storedHash;
    }
}