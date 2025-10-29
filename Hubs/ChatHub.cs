using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ChatBlitz.Data;
using ChatBlitz.Models;
using ChatBlitz.Services;
using Microsoft.EntityFrameworkCore;
using ChatBlitz.DTOs;

namespace ChatBlitz.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatBlitzContext _context;
        private readonly IChatService _chatService;
        private readonly IDirectMessageService _directMessageService; // new
        
        public ChatHub(ChatBlitzContext context, IChatService chatService, IDirectMessageService directMessageService)
        {
            _context = context;
            _chatService = chatService;
            _directMessageService = directMessageService; // new
        }
        
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                // Create user session
                var userSession = new UserSession
                {
                    UserId = userId.Value,
                    ConnectionId = Context.ConnectionId,
                    DeviceInfo = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString(),
                    IpAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IsActive = true
                };
                
                _context.UserSessions.Add(userSession);
                
                // Update user online status
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    user.IsOnline = true;
                    user.LastSeenAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                
                // Join user to their chat room groups
                var chatRooms = await _chatService.GetUserChatRoomsAsync(userId.Value);
                foreach (var chatRoom in chatRooms)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoom.Id}");
                }
                
                // Join conversation groups
                var convos = await _directMessageService.GetUserConversationsAsync(userId.Value);
                foreach (var convo in convos)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{convo.Id}");
                }

                var changed = "";
                
                // Notify other users that this user is online
                await Clients.Others.SendAsync("UserOnline", new { UserId = userId.Value });
            }
            
            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                // Remove user session
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.ConnectionId == Context.ConnectionId);
                
                if (session != null)
                {
                    _context.UserSessions.Remove(session);
                }
                
                // Check if user has other active sessions
                var hasOtherSessions = await _context.UserSessions
                    .AnyAsync(s => s.UserId == userId.Value && s.ConnectionId != Context.ConnectionId && s.IsActive);
                
                // If no other sessions, mark user as offline
                if (!hasOtherSessions)
                {
                    var user = await _context.Users.FindAsync(userId.Value);
                    if (user != null)
                    {
                        user.IsOnline = false;
                        user.LastSeenAt = DateTime.UtcNow;
                    }
                    
                    // Notify other users that this user is offline
                    await Clients.Others.SendAsync("UserOffline", new { UserId = userId.Value });
                }
                
                await _context.SaveChangesAsync();
            }
            
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task JoinChatRoom(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;
            
            if (await _chatService.IsUserInChatRoomAsync(userId.Value, chatRoomId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
                await Clients.Caller.SendAsync("JoinedChatRoom", chatRoomId);
            }
        }
        
        public async Task LeaveChatRoom(int chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            await Clients.Caller.SendAsync("LeftChatRoom", chatRoomId);
        }
        
        public async Task SendMessage(int chatRoomId, string content)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;
            
            var sendMessageDto = new DTOs.SendMessageDto
            {
                ChatRoomId = chatRoomId,
                Content = content
            };
            
            var messageDto = await _chatService.SendMessageAsync(userId.Value, sendMessageDto);
            if (messageDto != null)
            {
                // Send message to all users in the chat room
                await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("ReceiveMessage", messageDto);
            }
        }
        
        public async Task EditMessage(int messageId, string content)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;
            
            var editMessageDto = new DTOs.EditMessageDto
            {
                Content = content
            };
            
            var messageDto = await _chatService.EditMessageAsync(userId.Value, messageId, editMessageDto);
            if (messageDto != null)
            {
                // Send updated message to all users in the chat room
                await Clients.Group($"ChatRoom_{messageDto.ChatRoomId}").SendAsync("MessageEdited", messageDto);
            }
        }
        
        public async Task DeleteMessage(int messageId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;
            
            // Get message details before deletion
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId.Value);
            
            if (message != null)
            {
                var chatRoomId = message.ChatRoomId;
                var success = await _chatService.DeleteMessageAsync(userId.Value, messageId);
                
                if (success)
                {
                    // Notify all users in the chat room about message deletion
                    await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("MessageDeleted", new { MessageId = messageId, ChatRoomId = chatRoomId });
                }
            }
        }
        
        public async Task StartTyping(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;
            
            if (await _chatService.IsUserInChatRoomAsync(userId.Value, chatRoomId))
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    await Clients.GroupExcept($"ChatRoom_{chatRoomId}", Context.ConnectionId)
                        .SendAsync("UserStartedTyping", new { UserId = userId.Value, Username = user.Username, ChatRoomId = chatRoomId });
                }
            }
        }
        
        public async Task StopTyping(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return;
            
            if (await _chatService.IsUserInChatRoomAsync(userId.Value, chatRoomId))
            {
                await Clients.GroupExcept($"ChatRoom_{chatRoomId}", Context.ConnectionId)
                    .SendAsync("UserStoppedTyping", new { UserId = userId.Value, ChatRoomId = chatRoomId });
            }
        }
        
        public async Task SendDirectMessage(int receiverId, string content)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;
            var dto = new SendDirectMessageDto { ReceiverId = receiverId, Content = content };
            var message = await _directMessageService.SendDirectMessageAsync(userId.Value, dto);
            if (message == null) return;
            // Add both participants' active connections to conversation group
            if (message.ConversationId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{message.ConversationId.Value}");
                var receiverSessions = await _context.UserSessions
                    .Where(s => s.UserId == receiverId && s.IsActive)
                    .Select(s => s.ConnectionId)
                    .ToListAsync();
                foreach (var conn in receiverSessions)
                {
                    await Groups.AddToGroupAsync(conn, $"Conversation_{message.ConversationId.Value}");
                }
                // Send to receiver's connections and back to sender
                await Clients.Clients(receiverSessions).SendAsync("ReceiveDirectMessage", message);
                await Clients.Caller.SendAsync("ReceiveDirectMessage", message);
            }
        }
        
        private int? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}