using System;
using System.Collections.Generic;

namespace ChatBlitz.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public int User1Id { get; set; }
        public int User2Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual User User1 { get; set; } = null!;
        public virtual User User2 { get; set; } = null!;
        public virtual ICollection<DirectMessage> Messages { get; set; } = new List<DirectMessage>();
    }
}
