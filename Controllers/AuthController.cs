using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChatBlitz.Services;
using ChatBlitz.DTOs;
using System.Security.Claims;

namespace ChatBlitz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var result = await _authService.RegisterAsync(registerDto);
            if (result == null)
                return BadRequest(new { message = "Username or email already exists" });
            
            return Ok(result);
        }
        
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
                return Unauthorized(new { message = "Invalid credentials" });
            
            return Ok(result);
        }
        
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            // For logout, we'll just return success since JWT tokens are stateless
            // The client should remove the token from storage
            return Ok(new { message = "Logged out successfully" });
        }
        
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();
            
            var user = await _authService.GetUserByIdAsync(userId.Value);
            if (user == null)
                return NotFound();
            
            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                LastSeenAt = user.LastSeenAt,
                IsOnline = user.IsOnline
            };
            
            return Ok(userDto);
        }
        
        [HttpGet("check-username/{username}")]
        public async Task<IActionResult> CheckUsername(string username)
        {
            var isTaken = await _authService.IsUsernameTakenAsync(username);
            return Ok(new { isTaken });
        }
        
        [HttpGet("check-email/{email}")]
        public async Task<IActionResult> CheckEmail(string email)
        {
            var isTaken = await _authService.IsEmailTakenAsync(email);
            return Ok(new { isTaken });
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