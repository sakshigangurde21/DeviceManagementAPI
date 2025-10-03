using DeviceManagementAPI.Data;
using DeviceManagementAPI.DTO;
using DeviceManagementAPI.DTOs;
using DeviceManagementAPI.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace DeviceManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DeviceDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(DeviceDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest(new { message = "Username already exists" });

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = string.IsNullOrEmpty(dto.Role) ? "User" : char.ToUpper(dto.Role[0]) + dto.Role.Substring(1).ToLower()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User registered successfully" });
        }

        // POST: api/auth/login
        [HttpPost("login")] 
        public async Task<IActionResult> Login([FromBody] LoginDto dto) 
        { 
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username); 
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) 
                return Unauthorized(new { message = "Invalid username or password" }); 
            
            var token = GenerateJwtToken(user); 
            
            return Ok(new
            { 
                token, 
                username = user.Username, 
                role = user.Role
            }); 
        }
        private string GenerateJwtToken(User user)
        {
            var key = _config["Jwt:Key"] ?? "supersecretkey12345678901234567890";
            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
