using ChatBlitz.DTOs;
using ChatBlitz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatBlitz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DirectMessagesController : ControllerBase
    {
        private readonly IDirectMessageService _dmService;
        public DirectMessagesController(IDirectMessageService dmService)
        {
            _dmService = dmService;
        }

        [HttpGet("conversations")] public async Task<ActionResult<List<ConversationDto>>> GetConversations()
        {
            var userId = GetUserId(); if (!userId.HasValue) return Unauthorized();
            var convos = await _dmService.GetUserConversationsAsync(userId.Value);
            return Ok(convos);
        }

        [HttpGet("conversations/{otherUserId}/messages")] public async Task<ActionResult<List<DirectMessageDto>>> GetConversationMessages(int otherUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = GetUserId(); if (!userId.HasValue) return Unauthorized();
            var msgs = await _dmService.GetConversationMessagesAsync(userId.Value, otherUserId, page, pageSize);
            return Ok(msgs);
        }

        [HttpPost] public async Task<ActionResult<DirectMessageDto>> Send([FromBody] SendDirectMessageDto dto)
        {
            var userId = GetUserId(); if (!userId.HasValue) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var dm = await _dmService.SendDirectMessageAsync(userId.Value, dto);
            if (dm == null) return BadRequest(new { message = "Failed to send direct message" });
            return Ok(dm);
        }

        [HttpPost("{messageId}/read")] public async Task<IActionResult> MarkRead(int messageId)
        {
            var userId = GetUserId(); if (!userId.HasValue) return Unauthorized();
            var ok = await _dmService.MarkMessageReadAsync(userId.Value, messageId);
            if (!ok) return NotFound(new { message = "Message not found" });
            return Ok(new { message = "Message marked as read" });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var id)) return id; return null;
        }
    }
}
