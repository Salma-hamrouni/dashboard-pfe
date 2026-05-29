using System;
using System.Collections.Generic;

namespace DashboardAPI.Models
{
    public class Dashboard
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int DatasetId { get; set; }

        public int UserId { get; set; }

        public string ColumnsJson { get; set; } = string.Empty;
        public string InsightsJson { get; set; } = string.Empty;
        public string RecommendationsJson { get; set; } = string.Empty;

        public string ShareToken { get; set; } = string.Empty;

        public bool IsPublic { get; set; }

        // ✅ NOUVEAU
        public DateTime CreatedAt { get; set; }

        public List<Widget> Widgets { get; set; } = new List<Widget>();
    }
}