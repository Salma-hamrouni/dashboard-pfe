namespace DashboardAPI.Models;

public class Insight
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public int DatasetId { get; set; }
    public Dataset Dataset { get; set; } = null!;
}