using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardAPI.Models
{
    public class DashboardVersion
    {
        [Key]
        public int    Id          { get; set; }

        [Required]
        public int    DashboardId { get; set; }

        public int    Version     { get; set; }  // 1, 2, 3...

        [MaxLength(200)]
        public string? Label      { get; set; }  // ex: "Avant refonte", "v2 client"

        /// <summary>Snapshot JSON complet du dashboard + widgets au moment de la sauvegarde.</summary>
        public string SnapshotJson { get; set; } = string.Empty;

        public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(DashboardId))]
        public Dashboard? Dashboard { get; set; }
    }
}