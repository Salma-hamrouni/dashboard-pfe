using DashboardAPI.Models;

namespace DashboardAPI.DTOs
{
    public class DashboardDetailDto
    {
        public int     Id        { get; set; }
        public string  Name      { get; set; } = string.Empty;
        public int     DatasetId { get; set; }

        public List<string>? Columns         { get; set; } = new();
        public List<string>? Insights        { get; set; } = new();
        public List<string>? Recommendations { get; set; } = new();
        public List<Widget>? Widgets         { get; set; } = new();
    }
}