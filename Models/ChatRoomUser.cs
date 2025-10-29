namespace ChatBlitz.Models
{
    public class ChatRoomUser
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        public int ChatRoomId { get; set; }
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsAdmin { get; set; } = false;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ChatRoom ChatRoom { get; set; } = null!;
    }
}