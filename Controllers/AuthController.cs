using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using DashboardAPI.Models;
using DashboardAPI.Services;
using DashboardAPI.Data;
using DashboardAPI.DTOs.Auth;
using DashboardAPI.Common;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(
        AuthService authService,
        IConfiguration config,
        AppDbContext context,
        AuditService audit,
        ILogger<AuthController> logger) : ControllerBase
    {
        // ── POST /api/auth/register ───────────────────────────────────────────
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            var existing = await authService.GetByEmailAsync(dto.Email);
            if (existing != null)
                return Conflict(ApiResponse<object>.Fail("Un compte existe déjà avec cet email."));

            var user = await authService.Register(dto.Email, dto.Password, nameof(UserRole.Editor));

            await audit.LogAsync(user.Id, "USER_REGISTERED", "User", user.Id,
                $"email={dto.Email}", GetIp());

            logger.LogInformation("Nouvel utilisateur enregistré : {Email}", dto.Email);

            return CreatedAtAction(nameof(Me), new { id = user.Id },
                ApiResponse<UserInfoDto>.Ok(new UserInfoDto
                {
                    Id        = user.Id,
                    Email     = user.Email,
                    Name      = user.Name,
                    Role      = user.Role,
                    CreatedAt = user.CreatedAt
                }));
        }

        // ── POST /api/auth/login ──────────────────────────────────────────────
        /// <summary>Limité à 5 tentatives/minute par IP (protection brute-force).</summary>
        [HttpPost("login")]
        [EnableRateLimiting("auth-policy")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var user = await authService.Login(dto.Email, dto.Password);
            if (user == null)
            {
                logger.LogWarning("Tentative de connexion échouée pour {Email}", dto.Email);

                // Audit même en cas d'échec (userId=0 = non résolu)
                var failed = await authService.GetByEmailAsync(dto.Email);
                if (failed != null)
                    await audit.LogAsync(failed.Id, "USER_LOGIN_FAILED", "User", failed.Id,
                        $"email={dto.Email}", GetIp());

                return Unauthorized(ApiResponse<object>.Fail("Email ou mot de passe incorrect."));
            }

            var token = GenerateToken(user);

            await audit.LogAsync(user.Id, "USER_LOGIN", "User", user.Id,
                $"role={user.Role}", GetIp());

            logger.LogInformation("Connexion réussie : {Email} (rôle: {Role})", user.Email, user.Role);

            return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                Token = token,
                User  = new UserInfoDto
                {
                    Id        = user.Id,
                    Email     = user.Email,
                    Name      = user.Name,
                    Role      = user.Role,
                    CreatedAt = user.CreatedAt
                }
            }));
        }

        // ── GET /api/auth/me ──────────────────────────────────────────────────
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Me()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user   = await context.Users.FindAsync(userId);
            if (user == null) return NotFound(ApiResponse<object>.Fail("Utilisateur introuvable."));

            return Ok(ApiResponse<UserInfoDto>.Ok(new UserInfoDto
            {
                Id        = user.Id,
                Email     = user.Email,
                Role      = user.Role,
                CreatedAt = user.CreatedAt
            }));
        }

        // ── GET /api/auth/users ───────────────────────────────────────────────
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<UserInfoDto>>), 200)]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await context.Users
                .AsNoTracking()
                .OrderBy(u => u.CreatedAt)
                .Select(u => new UserInfoDto
                {
                    Id        = u.Id,
                    Email     = u.Email,
                    Name      = u.Name,
                    Role      = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<UserInfoDto>>.Ok(users, new { total = users.Count }));
        }

        // ── PUT /api/auth/users/{id}/role ─────────────────────────────────────
        [HttpPut("users/{id:int}/role")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequestDto dto)
        {
            var user = await context.Users.FindAsync(id);
            if (user == null)
                return NotFound(ApiResponse<object>.Fail("Utilisateur introuvable."));

            var oldRole = user.Role;
            user.Role   = dto.Role;
            await context.SaveChangesAsync();

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await audit.LogAsync(adminId, "USER_ROLE_CHANGED", "User", id,
                $"{oldRole} → {dto.Role}", GetIp());

            logger.LogInformation("Rôle changé : user {Id} {OldRole} → {NewRole}",
                id, oldRole, dto.Role);

            return Ok(ApiResponse<UserInfoDto>.Ok(new UserInfoDto
            {
                Id        = user.Id,
                Email     = user.Email,
                Role      = user.Role,
                CreatedAt = user.CreatedAt
            }));
        }

        // ── PUT /api/auth/update-name ────────────────────────────────────────
        [HttpPut("update-name")]
        [Authorize]
        public async Task<IActionResult> UpdateName([FromBody] UpdateNameRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(ApiResponse<object>.Fail("Le nom ne peut pas être vide."));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user   = await context.Users.FindAsync(userId);
            if (user == null) return NotFound(ApiResponse<object>.Fail("Utilisateur introuvable."));

            await context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Name, req.Name.Trim()));

            await audit.LogAsync(userId, "NAME_UPDATED", "User", userId, null, GetIp());

            return Ok(ApiResponse<object>.Ok(new { name = req.Name.Trim() }));
        }

        public record UpdateNameRequest(string Name);

        // ── Helpers ───────────────────────────────────────────────────────────
        private string GenerateToken(User user)
        {
            var key    = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);
            var expiry = config.GetValue("Jwt:ExpiryHours", 8);

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email,          user.Email),
                    new Claim(ClaimTypes.Role,           user.Role)
                ]),
                Expires            = DateTime.UtcNow.AddHours(expiry),
                Issuer             = config["Jwt:Issuer"],
                Audience           = config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        private string GetIp()
            => HttpContext.Request.Headers["X-Forwarded-For"]
                   .FirstOrDefault()?.Split(',')[0].Trim()
               ?? HttpContext.Connection.RemoteIpAddress?.ToString()
               ?? "unknown";
    }
}
