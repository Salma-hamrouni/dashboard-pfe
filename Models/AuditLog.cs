using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardAPI.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = "";     // LOGIN, CREATE_DASHBOARD, DELETE_WIDGET…

        [Required, MaxLength(100)]
        public string EntityType { get; set; } = ""; // Dashboard, Widget, DataSource…

        public int? EntityId { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }

        [MaxLength(45)]
        public string IpAddress { get; set; } = "";

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
