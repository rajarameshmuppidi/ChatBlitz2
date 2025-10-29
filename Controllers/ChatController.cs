using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChatBlitz.Services;
using ChatBlitz.DTOs;
using System.Security.Claims;

namespace ChatBlitz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }
        
        [HttpPost("rooms")]
        public async Task<ActionResult<ChatRoomDto>> CreateChatRoom([FromBody] CreateChatRoomDto createChatRoomDto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var chatRoom = await _chatService.CreateChatRoomAsync(userId.Value, createChatRoomDto);
            if (chatRoom == null)
                return BadRequest(new { message = "Failed to create chat room" });
            
            return CreatedAtAction(nameof(GetChatRoom), new { chatRoomId = chatRoom.Id }, chatRoom);
        }
        
        [HttpGet("rooms")]
        public async Task<ActionResult<List<ChatRoomDto>>> GetUserChatRooms()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            var chatRooms = await _chatService.GetUserChatRoomsAsync(userId.Value);
            return Ok(chatRooms);
        }
        
        [HttpGet("rooms/{chatRoomId}")]
        public async Task<ActionResult<ChatRoomDto>> GetChatRoom(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            var chatRoom = await _chatService.GetChatRoomAsync(chatRoomId, userId.Value);
            if (chatRoom == null)
                return NotFound();
            
            return Ok(chatRoom);
        }
        
        [HttpPost("rooms/{chatRoomId}/join")]
        public async Task<IActionResult> JoinChatRoom(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            var success = await _chatService.JoinChatRoomAsync(userId.Value, chatRoomId);
            if (!success)
                return BadRequest(new { message = "Failed to join chat room" });
            
            return Ok(new { message = "Successfully joined chat room" });
        }
        
        [HttpPost("rooms/{chatRoomId}/leave")]
        public async Task<IActionResult> LeaveChatRoom(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            var success = await _chatService.LeaveChatRoomAsync(userId.Value, chatRoomId);
            if (!success)
                return BadRequest(new { message = "Failed to leave chat room" });
            
            return Ok(new { message = "Successfully left chat room" });
        }
        
        [HttpGet("rooms/{chatRoomId}/messages")]
        public async Task<ActionResult<List<MessageDto>>> GetChatRoomMessages(
            int chatRoomId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            if (pageSize > 100) pageSize = 100; // Limit page size
            
            var messages = await _chatService.GetChatRoomMessagesAsync(chatRoomId, userId.Value, page, pageSize);
            return Ok(messages);
        }
        
        [HttpPost("messages")]
        public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageDto sendMessageDto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var message = await _chatService.SendMessageAsync(userId.Value, sendMessageDto);
            if (message == null)
                return BadRequest(new { message = "Failed to send message" });
            
            return Ok(message);
        }
        
        [HttpPut("messages/{messageId}")]
        public async Task<ActionResult<MessageDto>> EditMessage(int messageId, [FromBody] EditMessageDto editMessageDto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var message = await _chatService.EditMessageAsync(userId.Value, messageId, editMessageDto);
            if (message == null)
                return NotFound(new { message = "Message not found or you don't have permission to edit it" });
            
            return Ok(message);
        }
        
        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            var success = await _chatService.DeleteMessageAsync(userId.Value, messageId);
            if (!success)
                return NotFound(new { message = "Message not found or you don't have permission to delete it" });
            
            return Ok(new { message = "Message deleted successfully" });
        }
        
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}