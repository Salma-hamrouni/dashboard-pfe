using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DashboardAPI.Controllers
{
    [Authorize]
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Token invalide.");
            return int.Parse(claim);
        }

        protected string GetUserRole()
            => User.FindFirstValue(ClaimTypes.Role) ?? "Viewer";

        protected bool IsAdmin()  => GetUserRole() == "Admin";
        protected bool IsEditor() => GetUserRole() is "Admin" or "Editor";
        protected bool IsViewer() => GetUserRole() is "Admin" or "Editor" or "Viewer";
    }
}