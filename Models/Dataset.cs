namespace DashboardAPI.Models;

public class Dataset
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string ColumnsJson { get; set; } = "[]";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Dashboard> Dashboards { get; set; } = new List<Dashboard>();
    public ICollection<Insight> Insights { get; set; } = new List<Insight>();
}