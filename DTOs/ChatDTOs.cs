using System.ComponentModel.DataAnnotations;

namespace ChatBlitz.DTOs
{
    public class CreateChatRoomDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool IsPrivate { get; set; } = false;
    }
    
    public class ChatRoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public MessageDto? LastMessage { get; set; }
    }
    
    public class SendMessageDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public int ChatRoomId { get; set; }
    }
    
    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int SenderId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string? SenderDisplayName { get; set; }
        public int ChatRoomId { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
    }
    
    public class EditMessageDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }
}