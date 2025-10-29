using System.ComponentModel.DataAnnotations;

namespace ChatBlitz.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public int SenderId { get; set; }
        
        public int ChatRoomId { get; set; }
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        public bool IsEdited { get; set; } = false;
        
        public DateTime? EditedAt { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        // Navigation properties
        public virtual User Sender { get; set; } = null!;
        public virtual ChatRoom ChatRoom { get; set; } = null!;
    }
}