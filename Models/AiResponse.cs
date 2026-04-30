namespace DashboardAPI.Models
{
    public class AiResponse
    {
        public List<string> Insights        { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}