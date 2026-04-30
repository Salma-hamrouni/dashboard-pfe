using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardAPI.Models
{
    public enum DataSourceType
    {
        CSV = 0,
        SQL = 1,
        REST = 2
    }

    public class DataSource
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public DataSourceType Type { get; set; }

        // 🔥 FIX ICI
        [Column(TypeName = "longtext")]
        public string? ConnectionParamsJson { get; set; }

        [MaxLength(500)]
        public string? FilePath { get; set; }

        // 🔥 FIX ICI
        [Column(TypeName = "longtext")]
        public string? CachedDataJson { get; set; }

        // 🔥 FIX DATE (important MySQL)
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime")]
        public DateTime? LastRefreshedAt { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}