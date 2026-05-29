using DashboardAPI.Data;
using DashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
            => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User> Register(string email, string password, string role = "Editor")
        {
            var user = new User
            {
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role         = role,
                CreatedAt    = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;
            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
        }
    }
}