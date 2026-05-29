using System.Text.Json.Serialization;

namespace DashboardAPI.Models
{
    public class Widget
    {
        public int Id { get; set; }
        public int DashboardId { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Data { get; set; }

        public Dashboard? Dashboard { get; set; }
    }
}