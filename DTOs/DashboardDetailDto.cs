using System.Collections.Generic;
using DashboardAPI.Models;

namespace DashboardAPI.DTOs
{
   public class DashboardDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int DatasetId { get; set; } // ✅ ONLY THIS
    public List<string> Columns { get; set; }
    public List<string> Insights { get; set; }
    public List<string> Recommendations { get; set; }

    public List<Widget> Widgets { get; set; }
}
}