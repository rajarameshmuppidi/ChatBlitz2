using System.ComponentModel.DataAnnotations;

namespace ChatBlitz.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string? DisplayName { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
        
        public bool IsOnline { get; set; } = false;
        
        // Navigation properties
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<ChatRoomUser> ChatRoomUsers { get; set; } = new List<ChatRoomUser>();
        public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        // Direct messaging navigation collections
        public virtual ICollection<DirectMessage> DirectMessagesSent { get; set; } = new List<DirectMessage>();
        public virtual ICollection<DirectMessage> DirectMessagesReceived { get; set; } = new List<DirectMessage>();
        public virtual ICollection<Conversation> ConversationsAsUser1 { get; set; } = new List<Conversation>();
        public virtual ICollection<Conversation> ConversationsAsUser2 { get; set; } = new List<Conversation>();
    }
}