using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using DashboardAPI.Models;
using DashboardAPI.Services;
using DashboardAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService    _authService;
        private readonly IConfiguration _config;
        private readonly AppDbContext   _context;

        public AuthController(AuthService authService, IConfiguration config, AppDbContext context)
        {
            _authService = authService;
            _config      = config;
            _context     = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest request)
        {
            var existing = await _authService.GetByEmailAsync(request.Email);
            if (existing != null)
                return Conflict("Un compte existe déjà avec cet email.");

            // Le rôle par défaut est Viewer, sauf si spécifié (Admin uniquement)
            var role = nameof(UserRole.Viewer);
            var user = await _authService.Register(request.Email, request.Password, role);

            return CreatedAtAction(nameof(Register), new { id = user.Id }, new
            {
                user.Id,
                user.Email,
                user.Role
            });
        }

        //login 
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            var user = await _authService.Login(request.Email, request.Password);
            if (user == null)
                return Unauthorized("Email ou mot de passe incorrect.");

            var token = GenerateToken(user);
            return Ok(new
            {
                token,
                user = new { user.Id, user.Email, user.Role }
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user   = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new { user.Id, user.Email, user.Role, user.CreatedAt });
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, u.Email, u.Role, u.CreatedAt })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPut("users/{id:int}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest request)
        {
            var validRoles = new[] { "Admin", "Editor", "Viewer" };
            if (!validRoles.Contains(request.Role))
                return BadRequest($"Rôle invalide. Valeurs acceptées : {string.Join(", ", validRoles)}");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Role = request.Role;
            await _context.SaveChangesAsync();

            return Ok(new { user.Id, user.Email, user.Role });
        }

        private string GenerateToken(User user)
        {
            var key     = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var handler = new JwtSecurityTokenHandler();

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email,          user.Email),
                    new Claim(ClaimTypes.Role,           user.Role)
                }),
                Expires            = DateTime.UtcNow.AddHours(8),
                Issuer             = _config["Jwt:Issuer"],
                Audience           = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            return handler.WriteToken(handler.CreateToken(descriptor));
        }
    }

    public record AuthRequest(string Email, string Password);
    public record ChangeRoleRequest(string Role);
}