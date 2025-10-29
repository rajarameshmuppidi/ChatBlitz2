using System;
using System.ComponentModel.DataAnnotations;

namespace ChatBlitz.DTOs
{
    public class SendDirectMessageDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
        [Required]
        public int ReceiverId { get; set; }
    }

    public class DirectMessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int? ConversationId { get; set; }
    }

    public class ConversationDto
    {
        public int Id { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public DirectMessageDto? LastMessage { get; set; }
    }
}
