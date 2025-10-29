using System;
using System.ComponentModel.DataAnnotations;

namespace ChatBlitz.Models
{
    public class DirectMessage
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        // New: link to conversation
        public int? ConversationId { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual User Sender { get; set; } = null!;
        public virtual User Receiver { get; set; } = null!;
        public virtual Conversation? Conversation { get; set; }
    }
}
