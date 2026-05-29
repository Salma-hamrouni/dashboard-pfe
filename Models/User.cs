namespace DashboardAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Dataset> Datasets { get; set; } = new List<Dataset>();
    public ICollection<Dashboard> Dashboards { get; set; } = new List<Dashboard>();
}