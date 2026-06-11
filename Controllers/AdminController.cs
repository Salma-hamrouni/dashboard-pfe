using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Serialization;
using DashboardAPI.Data;
using DashboardAPI.Common;
using DashboardAPI.Models;
using DashboardAPI.Services;

namespace DashboardAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController(
        AppDbContext context,
        AuditService audit) : ControllerBase
    {
        private string GetIp()
            => HttpContext.Request.Headers["X-Forwarded-For"]
                   .FirstOrDefault()?.Split(',')[0].Trim()
               ?? HttpContext.Connection.RemoteIpAddress?.ToString()
               ?? "unknown";

        // ── GET /api/admin/users ──────────────────────────────────────────────
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await context.Users
                .AsNoTracking()
                .Select(u => new
                {
                    id             = u.Id,
                    email          = u.Email,
                    name           = u.Name,
                    role           = u.Role,
                    createdAt      = u.CreatedAt,
                    dashboardCount = context.Dashboards.Count(d => d.UserId == u.Id),
                    datasetCount   = context.Datasets.Count(d => d.UserId == u.Id),
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(users));
        }

        // ── POST /api/admin/users ─────────────────────────────────────────────
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserBody dto)
        {
            var emailNorm = dto.Email.Trim().ToLower();
            var exists    = await context.Users.AnyAsync(u => u.Email == emailNorm);
            if (exists)
                return BadRequest(ApiResponse<object>.Fail("Un compte avec cet email existe déjà."));

            var role = new[] { "Admin", "Editor", "Viewer" }.Contains(dto.Role) ? dto.Role : "Viewer";
            var user = new User
            {
                Email        = emailNorm,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role         = role,
                CreatedAt    = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try { await audit.LogAsync(adminId, "USER_CREATED", "User", user.Id, $"email={user.Email}|role={user.Role}", GetIp()); } catch { }

            return Ok(ApiResponse<object>.Ok(new
            {
                id             = user.Id,
                email          = user.Email,
                name           = user.Name,
                role           = user.Role,
                createdAt      = user.CreatedAt,
                dashboardCount = 0,
                datasetCount   = 0
            }));
        }

        // ── DELETE /api/admin/users/{id} ──────────────────────────────────────
        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (id == adminId)
                return BadRequest(ApiResponse<object>.Fail("Vous ne pouvez pas supprimer votre propre compte."));

            var user = await context.Users.FindAsync(id);
            if (user == null) return NotFound(ApiResponse<object>.Fail("Utilisateur introuvable."));

            context.Users.Remove(user);
            await context.SaveChangesAsync();

            try { await audit.LogAsync(adminId, "USER_DELETED", "User", id, $"email={user.Email}", GetIp()); } catch { }

            return Ok(ApiResponse<object>.Ok(new { message = "Utilisateur supprimé." }));
        }

        // ── PUT /api/admin/users/{id}/password ────────────────────────────────
        [HttpPut("users/{id:int}/password")]
        public async Task<IActionResult> ChangeUserPassword(int id, [FromBody] ChangePasswordBody dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return BadRequest(ApiResponse<object>.Fail("Le mot de passe doit contenir au moins 6 caractères."));

            var exists = await context.Users.AnyAsync(u => u.Id == id);
            if (!exists) return NotFound(ApiResponse<object>.Fail("Utilisateur introuvable."));

            await context.Users
                .Where(u => u.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(dto.Password)));

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try { await audit.LogAsync(adminId, "USER_PASSWORD_CHANGED", "User", id, null, GetIp()); } catch { }

            return Ok(ApiResponse<object>.Ok(new { message = "Mot de passe modifié." }));
        }

        // ── GET /api/admin/dashboards ─────────────────────────────────────────
        [HttpGet("dashboards")]
        public async Task<IActionResult> GetDashboards()
        {
            var dashboards = await context.Dashboards
                .AsNoTracking()
                .Select(d => new
                {
                    id          = d.Id,
                    name        = d.Name,
                    isPublic    = d.IsPublic,
                    createdAt   = d.CreatedAt,
                    userId      = d.UserId,
                    widgetCount = context.Widgets.Count(w => w.DashboardId == d.Id),
                    ownerEmail  = context.Users.Where(u => u.Id == d.UserId).Select(u => u.Email).FirstOrDefault() ?? "—",
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(dashboards));
        }

        // ── DELETE /api/admin/dashboards/{id} ─────────────────────────────────
        [HttpDelete("dashboards/{id:int}")]
        public async Task<IActionResult> DeleteDashboard(int id)
        {
            var dashboard = await context.Dashboards.FindAsync(id);
            if (dashboard == null) return NotFound(ApiResponse<object>.Fail("Dashboard introuvable."));

            context.Dashboards.Remove(dashboard);
            await context.SaveChangesAsync();

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try { await audit.LogAsync(adminId, "DASHBOARD_DELETED", "Dashboard", id, null, GetIp()); } catch { }

            return Ok(ApiResponse<object>.Ok(new { message = "Dashboard supprimé." }));
        }

        // ── GET /api/admin/widgets ────────────────────────────────────────────
        [HttpGet("widgets")]
        public async Task<IActionResult> GetWidgets()
        {
            var widgets = await context.Widgets
                .AsNoTracking()
                .Select(w => new
                {
                    id            = w.Id,
                    title         = w.Title,
                    type          = w.Type,
                    dashboardId   = w.DashboardId,
                    dashboardName = context.Dashboards.Where(d => d.Id == w.DashboardId).Select(d => d.Name).FirstOrDefault() ?? "—",
                    ownerEmail    = context.Dashboards
                                        .Where(d => d.Id == w.DashboardId)
                                        .Join(context.Users, d => d.UserId, u => u.Id, (d, u) => u.Email)
                                        .FirstOrDefault() ?? "—",
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(widgets));
        }

        // ── DELETE /api/admin/widgets/{id} ────────────────────────────────────
        [HttpDelete("widgets/{id:int}")]
        public async Task<IActionResult> DeleteWidget(int id)
        {
            var widget = await context.Widgets.FindAsync(id);
            if (widget == null) return NotFound(ApiResponse<object>.Fail("Widget introuvable."));

            context.Widgets.Remove(widget);
            await context.SaveChangesAsync();

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try { await audit.LogAsync(adminId, "WIDGET_DELETED", "Widget", id, null, GetIp()); } catch { }

            return Ok(ApiResponse<object>.Ok(new { message = "Widget supprimé." }));
        }

        // ── GET /api/admin/datasets ───────────────────────────────────────────
        [HttpGet("datasets")]
        public async Task<IActionResult> GetDatasets()
        {
            var datasets = await context.Datasets
                .AsNoTracking()
                .Select(d => new
                {
                    id         = d.Id,
                    fileName   = d.FileName,
                    rowCount   = d.RowCount,
                    uploadedAt = d.UploadedAt,
                    userId     = d.UserId,
                    ownerEmail = context.Users.Where(u => u.Id == d.UserId).Select(u => u.Email).FirstOrDefault() ?? "—",
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(datasets));
        }

        // ── DELETE /api/admin/datasets/{id} ───────────────────────────────────
        [HttpDelete("datasets/{id:int}")]
        public async Task<IActionResult> DeleteDataset(int id)
        {
            var dataset = await context.Datasets.FindAsync(id);
            if (dataset == null) return NotFound(ApiResponse<object>.Fail("Dataset introuvable."));

            context.Datasets.Remove(dataset);
            await context.SaveChangesAsync();

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try { await audit.LogAsync(adminId, "DATASET_DELETED", "Dataset", id, null, GetIp()); } catch { }

            return Ok(ApiResponse<object>.Ok(new { message = "Dataset supprimé." }));
        }

        // ── GET /api/admin/notifications ──────────────────────────────────────
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var notifs = await context.AuditLogs
                .AsNoTracking()
                .Where(a => a.UserId == adminId &&
                       (a.Action == "PASSWORD_RESET_REQUEST" || a.Action == "ACCOUNT_REQUEST"))
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    id        = a.Id,
                    action    = a.Action,
                    userId    = a.UserId,
                    userEmail = context.Users.Where(u => u.Id == a.UserId).Select(u => u.Email).FirstOrDefault() ?? "—",
                    details   = a.Details,
                    createdAt = a.CreatedAt,
                    ipAddress = a.IpAddress,
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(notifs));
        }

        // ── DELETE /api/admin/notifications/{id} ──────────────────────────────
        [HttpDelete("notifications/{id:int}")]
        public async Task<IActionResult> DismissNotification(int id)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var notif = await context.AuditLogs
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == adminId &&
                    (a.Action == "PASSWORD_RESET_REQUEST" || a.Action == "ACCOUNT_REQUEST"));

            if (notif == null) return NotFound(ApiResponse<object>.Fail("Notification introuvable."));

            context.AuditLogs.Remove(notif);
            await context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { message = "Notification supprimée." }));
        }

        // ── DTOs ──────────────────────────────────────────────────────────────
        public class CreateUserBody
        {
            [JsonPropertyName("email")]    public string Email    { get; set; } = "";
            [JsonPropertyName("password")] public string Password { get; set; } = "";
            [JsonPropertyName("role")]     public string Role     { get; set; } = "Viewer";
        }

        public class ChangePasswordBody
        {
            [JsonPropertyName("password")] public string Password { get; set; } = "";
        }
    }
}
