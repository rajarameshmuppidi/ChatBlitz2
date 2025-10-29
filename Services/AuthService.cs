using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ChatBlitz.Data;
using ChatBlitz.Models;
using ChatBlitz.DTOs;

namespace ChatBlitz.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<bool> LogoutAsync(int userId, string connectionId);
        Task<User?> GetUserByIdAsync(int userId);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<bool> IsEmailTakenAsync(string email);
    }
    
    public class AuthService : IAuthService
    {
        private readonly ChatBlitzContext _context;
        private readonly IJwtService _jwtService;
        
        public AuthService(ChatBlitzContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }
        
        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            // Check if username or email already exists
            if (await IsUsernameTakenAsync(registerDto.Username))
                return null;
                
            if (await IsEmailTakenAsync(registerDto.Email))
                return null;
            
            // Hash password
            var passwordHash = HashPassword(registerDto.Password);
            
            // Create user
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                DisplayName = registerDto.DisplayName ?? registerDto.Username,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Generate JWT token
            var token = _jwtService.GenerateToken(user);
            
            return new AuthResponseDto
            {
                Token = token,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }
        
        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            // Find user by email or username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.EmailOrUsername || u.Username == loginDto.EmailOrUsername);
            
            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                return null;
            
            // Update last seen
            user.LastSeenAt = DateTime.UtcNow;
            user.IsOnline = true;
            await _context.SaveChangesAsync();
            
            // Generate JWT token
            var token = _jwtService.GenerateToken(user);
            
            return new AuthResponseDto
            {
                Token = token,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }
        
        public async Task<bool> LogoutAsync(int userId, string connectionId)
        {
            // Remove user session
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ConnectionId == connectionId);
            
            if (session != null)
            {
                _context.UserSessions.Remove(session);
            }
            
            // Check if user has other active sessions
            var hasOtherSessions = await _context.UserSessions
                .AnyAsync(s => s.UserId == userId && s.ConnectionId != connectionId && s.IsActive);
            
            // If no other sessions, mark user as offline
            if (!hasOtherSessions)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeenAt = DateTime.UtcNow;
                }
            }
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }
        
        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }
        
        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
        
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
        
        private static bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }
        
        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                LastSeenAt = user.LastSeenAt,
                IsOnline = user.IsOnline
            };
        }
    }
}