using System.ComponentModel.DataAnnotations;

namespace ChatBlitz.Models
{
    public class UserSession
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string ConnectionId { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? DeviceInfo { get; set; }
        
        [StringLength(45)]
        public string? IpAddress { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}