using ChatBlitz.Data;
using ChatBlitz.DTOs;
using ChatBlitz.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatBlitz.Services
{
    public interface IDirectMessageService
    {
        Task<DirectMessageDto?> SendDirectMessageAsync(int senderId, SendDirectMessageDto dto);
        Task<List<DirectMessageDto>> GetConversationMessagesAsync(int userId, int otherUserId, int page = 1, int pageSize = 50);
        Task<List<ConversationDto>> GetUserConversationsAsync(int userId);
        Task<bool> MarkMessageReadAsync(int userId, int messageId);
    }

    public class DirectMessageService : IDirectMessageService
    {
        private readonly ChatBlitzContext _context;

        public DirectMessageService(ChatBlitzContext context)
        {
            _context = context;
        }

        private async Task<Conversation> GetOrCreateConversationAsync(int userId, int otherUserId)
        {
            if (userId == otherUserId)
                throw new InvalidOperationException("Cannot create conversation with self.");

            // Ensure ordering for uniqueness (User1Id < User2Id)
            var u1 = Math.Min(userId, otherUserId);
            var u2 = Math.Max(userId, otherUserId);

            var convo = await _context.Conversations
                .FirstOrDefaultAsync(c => c.User1Id == u1 && c.User2Id == u2);
            if (convo != null) return convo;

            convo = new Conversation
            {
                User1Id = u1,
                User2Id = u2,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _context.Conversations.Add(convo);
            await _context.SaveChangesAsync();
            return convo;
        }

        public async Task<DirectMessageDto?> SendDirectMessageAsync(int senderId, SendDirectMessageDto dto)
        {
            var receiver = await _context.Users.FindAsync(dto.ReceiverId);
            if (receiver == null) return null;
            if (receiver.Id == senderId) return null;

            var conversation = await GetOrCreateConversationAsync(senderId, receiver.Id);

            var dm = new DirectMessage
            {
                Content = dto.Content,
                SenderId = senderId,
                ReceiverId = receiver.Id,
                ConversationId = conversation.Id,
                SentAt = DateTime.UtcNow
            };
            _context.DirectMessages.Add(dm);
            conversation.LastMessageAt = dm.SentAt;
            await _context.SaveChangesAsync();

            return new DirectMessageDto
            {
                Id = dm.Id,
                Content = dm.Content,
                SenderId = dm.SenderId,
                ReceiverId = dm.ReceiverId,
                SentAt = dm.SentAt,
                IsRead = dm.IsRead,
                ReadAt = dm.ReadAt,
                IsEdited = dm.IsEdited,
                EditedAt = dm.EditedAt,
                IsDeleted = dm.IsDeleted,
                ConversationId = dm.ConversationId
            };
        }

        public async Task<List<DirectMessageDto>> GetConversationMessagesAsync(int userId, int otherUserId, int page = 1, int pageSize = 50)
        {
            if (pageSize > 100) pageSize = 100;
            var u1 = Math.Min(userId, otherUserId);
            var u2 = Math.Max(userId, otherUserId);
            var convo = await _context.Conversations.FirstOrDefaultAsync(c => c.User1Id == u1 && c.User2Id == u2);
            if (convo == null) return new List<DirectMessageDto>();

            return await _context.DirectMessages
                .Where(m => m.ConversationId == convo.Id && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new DirectMessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead,
                    ReadAt = m.ReadAt,
                    IsEdited = m.IsEdited,
                    EditedAt = m.EditedAt,
                    IsDeleted = m.IsDeleted,
                    ConversationId = m.ConversationId
                })
                .Reverse()
                .ToListAsync();
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(int userId)
        {
            return await _context.Conversations
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .Select(c => new ConversationDto
                {
                    Id = c.Id,
                    User1Id = c.User1Id,
                    User2Id = c.User2Id,
                    CreatedAt = c.CreatedAt,
                    LastMessageAt = c.LastMessageAt,
                    LastMessage = c.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Take(1)
                        .Select(m => new DirectMessageDto
                        {
                            Id = m.Id,
                            Content = m.Content,
                            SenderId = m.SenderId,
                            ReceiverId = m.ReceiverId,
                            SentAt = m.SentAt,
                            IsRead = m.IsRead,
                            ReadAt = m.ReadAt,
                            IsEdited = m.IsEdited,
                            EditedAt = m.EditedAt,
                            IsDeleted = m.IsDeleted,
                            ConversationId = m.ConversationId
                        }).FirstOrDefault()
                })
                .ToListAsync();
        }

        public async Task<bool> MarkMessageReadAsync(int userId, int messageId)
        {
            var msg = await _context.DirectMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId);
            if (msg == null || msg.IsRead) return false;
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
