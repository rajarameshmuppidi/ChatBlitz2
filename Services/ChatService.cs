using Microsoft.EntityFrameworkCore;
using ChatBlitz.Data;
using ChatBlitz.Models;
using ChatBlitz.DTOs;

namespace ChatBlitz.Services
{
    public interface IChatService
    {
        Task<ChatRoomDto?> CreateChatRoomAsync(int userId, CreateChatRoomDto createChatRoomDto);
        Task<bool> JoinChatRoomAsync(int userId, int chatRoomId);
        Task<bool> LeaveChatRoomAsync(int userId, int chatRoomId);
        Task<List<ChatRoomDto>> GetUserChatRoomsAsync(int userId);
        Task<List<MessageDto>> GetChatRoomMessagesAsync(int chatRoomId, int userId, int page = 1, int pageSize = 50);
        Task<MessageDto?> SendMessageAsync(int userId, SendMessageDto sendMessageDto);
        Task<MessageDto?> EditMessageAsync(int userId, int messageId, EditMessageDto editMessageDto);
        Task<bool> DeleteMessageAsync(int userId, int messageId);
        Task<bool> IsUserInChatRoomAsync(int userId, int chatRoomId);
        Task<ChatRoomDto?> GetChatRoomAsync(int chatRoomId, int userId);
    }
    
    public class ChatService : IChatService
    {
        private readonly ChatBlitzContext _context;
        
        public ChatService(ChatBlitzContext context)
        {
            _context = context;
        }
        
        public async Task<ChatRoomDto?> CreateChatRoomAsync(int userId, CreateChatRoomDto createChatRoomDto)
        {
            var chatRoom = new ChatRoom
            {
                Name = createChatRoomDto.Name,
                Description = createChatRoomDto.Description,
                IsPrivate = createChatRoomDto.IsPrivate,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.ChatRooms.Add(chatRoom);
            await _context.SaveChangesAsync();
            
            // Add creator as admin member
            var chatRoomUser = new ChatRoomUser
            {
                UserId = userId,
                ChatRoomId = chatRoom.Id,
                IsAdmin = true,
                JoinedAt = DateTime.UtcNow
            };
            
            _context.ChatRoomUsers.Add(chatRoomUser);
            await _context.SaveChangesAsync();
            
            return await GetChatRoomAsync(chatRoom.Id, userId);
        }
        
        public async Task<bool> JoinChatRoomAsync(int userId, int chatRoomId)
        {
            // Check if user is already in the chat room
            if (await IsUserInChatRoomAsync(userId, chatRoomId))
                return false;
            
            // Check if chat room exists
            var chatRoom = await _context.ChatRooms.FindAsync(chatRoomId);
            if (chatRoom == null)
                return false;
            
            var chatRoomUser = new ChatRoomUser
            {
                UserId = userId,
                ChatRoomId = chatRoomId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow
            };
            
            _context.ChatRoomUsers.Add(chatRoomUser);
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        public async Task<bool> LeaveChatRoomAsync(int userId, int chatRoomId)
        {
            var chatRoomUser = await _context.ChatRoomUsers
                .FirstOrDefaultAsync(cru => cru.UserId == userId && cru.ChatRoomId == chatRoomId);
            
            if (chatRoomUser == null)
                return false;
            
            _context.ChatRoomUsers.Remove(chatRoomUser);
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        public async Task<List<ChatRoomDto>> GetUserChatRoomsAsync(int userId)
        {
            return await _context.ChatRoomUsers
                .Where(cru => cru.UserId == userId)
                .Include(cru => cru.ChatRoom)
                .ThenInclude(cr => cr.CreatedByUser)
                .Include(cru => cru.ChatRoom)
                .ThenInclude(cr => cr.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .ThenInclude(m => m.Sender)
                .Select(cru => new ChatRoomDto
                {
                    Id = cru.ChatRoom.Id,
                    Name = cru.ChatRoom.Name,
                    Description = cru.ChatRoom.Description,
                    IsPrivate = cru.ChatRoom.IsPrivate,
                    CreatedByUserId = cru.ChatRoom.CreatedByUserId,
                    CreatedByUsername = cru.ChatRoom.CreatedByUser.Username,
                    CreatedAt = cru.ChatRoom.CreatedAt,
                    MemberCount = cru.ChatRoom.ChatRoomUsers.Count,
                    LastMessage = cru.ChatRoom.Messages.OrderByDescending(m => m.SentAt).Take(1)
                        .Select(m => new MessageDto
                        {
                            Id = m.Id,
                            Content = m.Content,
                            SenderId = m.SenderId,
                            SenderUsername = m.Sender.Username,
                            SenderDisplayName = m.Sender.DisplayName,
                            ChatRoomId = m.ChatRoomId,
                            SentAt = m.SentAt,
                            IsEdited = m.IsEdited,
                            EditedAt = m.EditedAt
                        }).FirstOrDefault()
                })
                .ToListAsync();
        }
        
        public async Task<List<MessageDto>> GetChatRoomMessagesAsync(int chatRoomId, int userId, int page = 1, int pageSize = 50)
        {
            if (!await IsUserInChatRoomAsync(userId, chatRoomId))
                return new List<MessageDto>();
            
            return await _context.Messages
                .Where(m => m.ChatRoomId == chatRoomId && !m.IsDeleted)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    SenderUsername = m.Sender.Username,
                    SenderDisplayName = m.Sender.DisplayName,
                    ChatRoomId = m.ChatRoomId,
                    SentAt = m.SentAt,
                    IsEdited = m.IsEdited,
                    EditedAt = m.EditedAt
                })
                .Reverse()
                .ToListAsync();
        }
        
        public async Task<MessageDto?> SendMessageAsync(int userId, SendMessageDto sendMessageDto)
        {
            if (!await IsUserInChatRoomAsync(userId, sendMessageDto.ChatRoomId))
                return null;
            
            var message = new Message
            {
                Content = sendMessageDto.Content,
                SenderId = userId,
                ChatRoomId = sendMessageDto.ChatRoomId,
                SentAt = DateTime.UtcNow
            };
            
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            
            // Load the message with sender information
            var messageWithSender = await _context.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == message.Id);
            
            if (messageWithSender == null)
                return null;
            
            return new MessageDto
            {
                Id = messageWithSender.Id,
                Content = messageWithSender.Content,
                SenderId = messageWithSender.SenderId,
                SenderUsername = messageWithSender.Sender.Username,
                SenderDisplayName = messageWithSender.Sender.DisplayName,
                ChatRoomId = messageWithSender.ChatRoomId,
                SentAt = messageWithSender.SentAt,
                IsEdited = messageWithSender.IsEdited,
                EditedAt = messageWithSender.EditedAt
            };
        }
        
        public async Task<MessageDto?> EditMessageAsync(int userId, int messageId, EditMessageDto editMessageDto)
        {
            var message = await _context.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId && !m.IsDeleted);
            
            if (message == null)
                return null;
            
            message.Content = editMessageDto.Content;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return new MessageDto
            {
                Id = message.Id,
                Content = message.Content,
                SenderId = message.SenderId,
                SenderUsername = message.Sender.Username,
                SenderDisplayName = message.Sender.DisplayName,
                ChatRoomId = message.ChatRoomId,
                SentAt = message.SentAt,
                IsEdited = message.IsEdited,
                EditedAt = message.EditedAt
            };
        }
        
        public async Task<bool> DeleteMessageAsync(int userId, int messageId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);
            
            if (message == null)
                return false;
            
            message.IsDeleted = true;
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        public async Task<bool> IsUserInChatRoomAsync(int userId, int chatRoomId)
        {
            return await _context.ChatRoomUsers
                .AnyAsync(cru => cru.UserId == userId && cru.ChatRoomId == chatRoomId);
        }
        
        public async Task<ChatRoomDto?> GetChatRoomAsync(int chatRoomId, int userId)
        {
            if (!await IsUserInChatRoomAsync(userId, chatRoomId))
                return null;
            
            return await _context.ChatRooms
                .Where(cr => cr.Id == chatRoomId)
                .Include(cr => cr.CreatedByUser)
                .Include(cr => cr.ChatRoomUsers)
                .Include(cr => cr.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .ThenInclude(m => m.Sender)
                .Select(cr => new ChatRoomDto
                {
                    Id = cr.Id,
                    Name = cr.Name,
                    Description = cr.Description,
                    IsPrivate = cr.IsPrivate,
                    CreatedByUserId = cr.CreatedByUserId,
                    CreatedByUsername = cr.CreatedByUser.Username,
                    CreatedAt = cr.CreatedAt,
                    MemberCount = cr.ChatRoomUsers.Count,
                    LastMessage = cr.Messages.OrderByDescending(m => m.SentAt).Take(1)
                        .Select(m => new MessageDto
                        {
                            Id = m.Id,
                            Content = m.Content,
                            SenderId = m.SenderId,
                            SenderUsername = m.Sender.Username,
                            SenderDisplayName = m.Sender.DisplayName,
                            ChatRoomId = m.ChatRoomId,
                            SentAt = m.SentAt,
                            IsEdited = m.IsEdited,
                            EditedAt = m.EditedAt
                        }).FirstOrDefault()
                })
                .FirstOrDefaultAsync();
        }
    }
}