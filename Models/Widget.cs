using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardAPI.Models
{
    public class Widget
    {
        public int     Id          { get; set; }
        public string  Type        { get; set; } = string.Empty;
        public string  Title       { get; set; } = string.Empty;
        public string? Data        { get; set; }
        public string? Config      { get; set; }
        public int     DashboardId { get; set; }

        [ForeignKey(nameof(DashboardId))]
        public Dashboard? Dashboard { get; set; }
    }
}