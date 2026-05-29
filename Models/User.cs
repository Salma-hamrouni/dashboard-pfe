namespace DashboardAPI.Models
{
    public enum UserRole
    {
        Admin  = 0,
        Editor = 1,
        Viewer = 2
    }

    public class User
    {
        public int      Id           { get; set; }
        public string   Email        { get; set; } = string.Empty;
        public string   PasswordHash { get; set; } = string.Empty;
        public string   Role         { get; set; } = nameof(UserRole.Editor);
        public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

        public ICollection<Dataset>   Datasets   { get; set; } = new List<Dataset>();
        public ICollection<Dashboard> Dashboards { get; set; } = new List<Dashboard>();
    }
}