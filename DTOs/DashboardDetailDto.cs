using WidgetModel = DashboardAPI.Models.Widget;

namespace DashboardAPI.DTOs
{
    public class DashboardDetailDto
    {
        public int     Id        { get; set; }
        public string  Name      { get; set; } = string.Empty;
        public int?    DatasetId { get; set; }

        public List<string>?      Columns         { get; set; } = [];
        public List<string>?      Insights        { get; set; } = [];
        public List<string>?      Recommendations { get; set; } = [];
        public List<WidgetModel>? Widgets         { get; set; } = [];
    }
}