using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardAPI.Models
{
    public enum SharePermission
    {
        View = 0,   
        Edit = 1    
    }

    public class DashboardShare
    {
        [Key]
        public int    Id           { get; set; }

        [Required]
        public int    DashboardId  { get; set; }
        public int?   SharedWithUserId { get; set; }

       
        [MaxLength(100)]
        public string Token        { get; set; } = Guid.NewGuid().ToString();

        public SharePermission Permission { get; set; } = SharePermission.View;

        public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        // Navigation
        [ForeignKey(nameof(DashboardId))]
        public Dashboard? Dashboard { get; set; }

        [ForeignKey(nameof(SharedWithUserId))]
        public User? SharedWithUser { get; set; }
    }
}