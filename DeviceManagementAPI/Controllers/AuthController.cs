using DeviceManagementAPI.Data;
using DeviceManagementAPI.DTO;
using DeviceManagementAPI.DTOs;
using DeviceManagementAPI.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // ✅ Validate username & password
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Length < 2 || dto.Username.Length > 20)
                return BadRequest(new { message = "Username must be 2–20 characters long." });

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6 || dto.Password.Length > 32)
                return BadRequest(new { message = "Password must be 6–32 characters long." });

            // ✅ Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return Conflict(new { message = "Username already exists" }); // <-- 409

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = string.IsNullOrEmpty(dto.Role)
                    ? "User"
                    : char.ToUpper(dto.Role[0]) + dto.Role.Substring(1).ToLower()
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

            if (user == null)
                return NotFound(new { message = "User not found" }); // <-- 404

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid password" }); // <-- 401


            // Generate short-lived access token
            var accessToken = GenerateJwtToken(user, 15); // 15 mins
            // Generate refresh token and store in DB
            var refreshToken = CreateRefreshToken(Request.HttpContext.Connection.RemoteIpAddress?.ToString());

            var refreshEntity = new RefreshToken
            {
                Token = refreshToken.Token,
                Expires = refreshToken.Expires,
                Created = refreshToken.Created,
                CreatedByIp = refreshToken.CreatedByIp,
                UserId = user.Id
            };

            _context.RefreshTokens.Add(refreshEntity);
            await _context.SaveChangesAsync();

            // Set cookies
            Response.Cookies.Append("jwt", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None, // needed if frontend runs on different port
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", refreshEntity.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = refreshEntity.Expires
            });

            return Ok(new { message = "Login successful", username = user.Username, role = user.Role });
        }


        // POST: api/auth/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var token))
                return Unauthorized(new { message = "No refresh token provided" });

            var existing = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == token);

            if (existing == null || !existing.IsActive)
                return Unauthorized(new { message = "Invalid or expired refresh token" });

            // Rotate refresh token
            var newRefresh = CreateRefreshToken(Request.HttpContext.Connection.RemoteIpAddress?.ToString());

            existing.Revoked = DateTime.UtcNow;
            existing.RevokedByIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            existing.ReplacedByToken = newRefresh.Token;

            var newRefreshEntity = new RefreshToken
            {
                Token = newRefresh.Token,
                Expires = newRefresh.Expires,
                Created = newRefresh.Created,
                CreatedByIp = newRefresh.CreatedByIp,
                UserId = existing.UserId
            };

            _context.RefreshTokens.Add(newRefreshEntity);
            await _context.SaveChangesAsync();

            // Generate new JWT
            var accessToken = GenerateJwtToken(existing.User!, 15);

            // Set new cookies
            Response.Cookies.Append("jwt", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(1)
            });

            Response.Cookies.Append("refreshToken", newRefreshEntity.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = newRefreshEntity.Expires
            });

            return Ok(new { message = "Token refreshed successfully" });
        }


        // GET: api/auth/profile
        [Authorize]
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var username = User.Identity?.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst("UserId")?.Value;

            return Ok(new { userId, username, role });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("refreshToken", out var token))
            {
                var existing = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
                if (existing != null && existing.Revoked == null)
                {
                    existing.Revoked = DateTime.UtcNow;
                    existing.RevokedByIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _context.SaveChangesAsync();
                }

                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None
                });
            }

            if (Request.Cookies.ContainsKey("jwt"))
            {
                Response.Cookies.Delete("jwt", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None
                });
            }

            return Ok(new { message = "Logged out successfully" });
        }


        // ================= Helper Methods =================

        private string GenerateJwtToken(User user, int minutesValid)
        {
            var key = _config["Jwt:Key"] ?? "supersecretkey12345678901234567890";
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
        new Claim("UserId", user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutesValid),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private (string Token, DateTime Expires, DateTime Created, string? CreatedByIp) CreateRefreshToken(string? ipAddress)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var token = Convert.ToBase64String(randomBytes);
            var created = DateTime.UtcNow;
            var expires = created.AddDays(7); // Refresh token valid for 7 days

            return (token, expires, created, ipAddress);
        }
    }
}