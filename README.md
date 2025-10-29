# ChatBlitz Web API

A real-time chat application web API built with .NET 8, featuring JWT authentication, SignalR for real-time messaging, and support for multiple device logins.

## Features

- **JWT Authentication**: Secure user authentication with JSON Web Tokens
- **Real-time Messaging**: SignalR-based real-time chat functionality
- **Multiple Device Support**: Users can be logged in from multiple devices simultaneously
- **RESTful API**: Clean REST API for chat operations
- **Entity Framework Core**: Database support with SQL Server and In-Memory database fallback
- **Swagger UI**: Interactive API documentation

## Technologies Used

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8.0.10
- SignalR
- JWT Authentication
- SQL Server / In-Memory Database
- Swagger/OpenAPI

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (optional, uses In-Memory database by default)

### Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-is-at-least-256-bits-long-for-security",
    "Issuer": "ChatBlitz",
    "Audience": "ChatBlitzUsers"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ChatBlitzDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### Running the Application

1. **Build the application:**
   ```bash
   dotnet build
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Access Swagger UI:**
   - Navigate to: `https://localhost:7107/swagger` or `http://localhost:5190/swagger`
   - Or use the launch profile from Visual Studio

## API Endpoints

### Authentication

- **POST** `/api/auth/register` - Register a new user
- **POST** `/api/auth/login` - Login user
- **POST** `/api/auth/logout` - Logout user
- **GET** `/api/auth/me` - Get current user info
- **GET** `/api/auth/check-username/{username}` - Check if username is available
- **GET** `/api/auth/check-email/{email}` - Check if email is available

### Chat

- **POST** `/api/chat/rooms` - Create a new chat room
- **GET** `/api/chat/rooms` - Get user's chat rooms
- **GET** `/api/chat/rooms/{chatRoomId}` - Get specific chat room
- **POST** `/api/chat/rooms/{chatRoomId}/join` - Join a chat room
- **POST** `/api/chat/rooms/{chatRoomId}/leave` - Leave a chat room
- **GET** `/api/chat/rooms/{chatRoomId}/messages` - Get chat room messages (paginated)
- **POST** `/api/chat/messages` - Send a message
- **PUT** `/api/chat/messages/{messageId}` - Edit a message
- **DELETE** `/api/chat/messages/{messageId}` - Delete a message

### SignalR Hub

- **Hub URL**: `/chathub`

#### Hub Methods (Client to Server)

- `JoinChatRoom(int chatRoomId)` - Join a chat room
- `LeaveChatRoom(int chatRoomId)` - Leave a chat room
- `SendMessage(int chatRoomId, string content)` - Send a message
- `EditMessage(int messageId, string content)` - Edit a message
- `DeleteMessage(int messageId)` - Delete a message
- `StartTyping(int chatRoomId)` - Indicate user is typing
- `StopTyping(int chatRoomId)` - Indicate user stopped typing

#### Hub Events (Server to Client)

- `ReceiveMessage(MessageDto message)` - Receive a new message
- `MessageEdited(MessageDto message)` - Receive edited message
- `MessageDeleted(object data)` - Receive message deletion notification
- `UserOnline(object data)` - User came online
- `UserOffline(object data)` - User went offline
- `UserStartedTyping(object data)` - User started typing
- `UserStoppedTyping(object data)` - User stopped typing
- `JoinedChatRoom(int chatRoomId)` - Joined chat room confirmation
- `LeftChatRoom(int chatRoomId)` - Left chat room confirmation

## Authentication Flow

1. **Register**: Create a new account with username, email, and password
2. **Login**: Authenticate and receive a JWT token
3. **Use Token**: Include the token in the Authorization header: `Bearer <token>`
4. **SignalR Connection**: Pass token as query parameter: `?access_token=<token>`

## Database Migrations

The application auto-migrates the database on startup. To create migrations manually:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Multiple Device Support

Users can log in from multiple devices simultaneously. The application tracks:
- Each device connection via SignalR ConnectionId
- Device information from User-Agent
- IP addresses
- Active sessions per user
- User goes offline only when all sessions are terminated

## Models

### User
- Id, Username, Email, PasswordHash
- DisplayName, CreatedAt, LastSeenAt, IsOnline
- SentMessages, ChatRoomUsers, UserSessions

### ChatRoom
- Id, Name, Description, IsPrivate
- CreatedByUserId, CreatedAt
- Messages, ChatRoomUsers

### Message
- Id, Content, SenderId, ChatRoomId
- SentAt, IsEdited, EditedAt, IsDeleted

### ChatRoomUser
- Id, UserId, ChatRoomId
- JoinedAt, IsAdmin

### UserSession
- Id, UserId, ConnectionId
- DeviceInfo, IpAddress
- CreatedAt, LastActivityAt, IsActive

## Security Features

- Password hashing using SHA256
- JWT token validation
- Authorization on all protected endpoints
- CORS configuration for cross-origin requests
- Token expiration (7 days default)

## Development

- **Framework**: .NET 8
- **Language**: C# 12.0
- **Database**: Entity Framework Core with SQL Server / In-Memory
- **API Documentation**: Swagger/OpenAPI

## CORS Configuration

Two CORS policies are configured:
- **AllowAll**: Allows any origin, method, and header (for development)
- **SignalRCors**: Configured for SignalR with credentials support

## Production Notes

**Important**: Before deploying to production:

1. Change the JWT SecretKey in `appsettings.json`
2. Update CORS policies to restrict allowed origins
3. Use SQL Server instead of In-Memory database
4. Enable HTTPS
5. Implement proper logging
6. Add rate limiting
7. Implement input validation and sanitization
8. Add comprehensive error handling

## License

This project is created for educational purposes.

## Author

ChatBlitz Team
