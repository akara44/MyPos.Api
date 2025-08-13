using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyPos.Infrastructure.Persistence;
using MyPos.Application.Dtos;
using MyPos.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyPos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MyPosDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(MyPosDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(x => x.Email == dto.Email))
            return BadRequest("Bu e-posta zaten kullanılıyor.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Şifre gereklidir.");

        CreatePasswordHash(dto.Password, out byte[] hash, out byte[] salt);

        var user = new User
        {
            FirstName = dto.FirstName ?? string.Empty,
            LastName = dto.LastName ?? string.Empty,
            CompanyName = dto.CompanyName ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            City = dto.City ?? string.Empty,
            Email = dto.Email ?? string.Empty,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        string token = CreateToken(user);
        return Ok(new { Token = token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null ||
            dto.Password == null ||
            user.PasswordHash == null ||
            user.PasswordSalt == null ||
            !VerifyPasswordHash(dto.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized("Geçersiz e-posta ya da şifre.");

        string token = CreateToken(user);
        return Ok(new { Token = token });
    }

    private void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private bool VerifyPasswordHash(string password, byte[] hash, byte[] salt)
    {
        using var hmac = new HMACSHA512(salt);
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computed.SequenceEqual(hash);
    }

    private string CreateToken(User user)
    {
        // JWT Key kontrolü eklendi
        var jwtKey = _config["Jwt:Key"] ?? throw new ApplicationException("JWT Key configuration is missing");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("Company", user.CompanyName ?? string.Empty),
            new Claim("City", user.City ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    [HttpPost("personnel-login")]
    public async Task<IActionResult> PersonnelLogin([FromBody] PersonnelLoginDto dto)
    {
        var personnel = await _context.Personnel.FirstOrDefaultAsync(p => p.Code == dto.Code);
        if (personnel == null || string.IsNullOrEmpty(dto.Password) || string.IsNullOrEmpty(personnel.PasswordHash))
            return Unauthorized("Geçersiz kullanıcı kodu ya da şifre.");

        // BCrypt ile parola doğrulama
        if (!VerifyPersonnelPassword(dto.Password, personnel.PasswordHash))
            return Unauthorized("Geçersiz kullanıcı kodu ya da şifre.");

        string token = CreatePersonnelToken(personnel);
        return Ok(new { Token = token });
    }

    private bool VerifyPersonnelPassword(string password, string storedHash)
    {
        
        return BCrypt.Net.BCrypt.Verify(password, storedHash);
    }

    private string CreatePersonnelToken(Personnel personnel)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new ApplicationException("JWT Key configuration is missing");

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, personnel.Id.ToString()),
        new Claim(ClaimTypes.Name, personnel.Name ?? string.Empty),
        new Claim("Role", personnel.Role ?? string.Empty),
        new Claim("Phone", personnel.Phone ?? string.Empty),

        
        new Claim("ViewCustomer", personnel.ViewCustomer.ToString()),
        new Claim("AddOrUpdateProduct", personnel.AddOrUpdateProduct.ToString()),
        new Claim("DeleteProduct", personnel.DeleteProduct.ToString())
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

}