# ?? JWT Authentication Scheme - Manual Update Guide

## What Needs to Be Changed

Your current Program.cs has a basic authentication registration but is **missing the JWT Bearer configuration** that validates tokens.

## Current Problem (Line 29)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
```

This line only registers authentication but doesn't configure how to validate JWT tokens.

## Solution: Replace Line 29 with Complete JWT Configuration

### Step-by-Step Instructions

1. **Open Program.cs** in Visual Studio
2. **Find line 29** (after the `var key = Encoding.UTF8.GetBytes(secretKey);` line)
3. **Delete this single line:**
   ```csharp
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
   ```

4. **Replace it with this complete configuration:**

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"] ?? "ChatBlitz",
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"] ?? "ChatBlitzUsers",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    // Configure JWT authentication for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        }
    };
});
```

## Additional Optional Improvements

### 1. Update Swagger Configuration (Line 53)

**Find:**
```csharp
builder.Services.AddSwaggerGen();
```

**Replace with:**
```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ChatBlitz API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

This adds the "Authorize" button in Swagger UI where you can enter your JWT token.

### 2. Update Swagger Middleware Order (Lines 57-62)

**Find:**
```csharp
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
```

**Replace with:**
```csharp
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
```

This ensures Swagger is only enabled in development and middleware is in correct order.

### 3. Add CORS to SignalR Hub (Line 65)

**Find:**
```csharp
app.MapHub<ChatHub>("/chathub");
```

**Replace with:**
```csharp
app.MapHub<ChatHub>("/chathub").RequireCors("SignalRCors");
```

## Quick Copy-Paste Option

I've created a file called **`Program_FINAL.cs`** in your workspace with all these changes already applied.

**You can:**
1. Open `Program_FINAL.cs`
2. Copy ALL the content
3. Open `Program.cs`
4. Replace EVERYTHING with the content from `Program_FINAL.cs`
5. Delete `Program_FINAL.cs` (it's just a reference file)

## What This Fixes

? **JWT Token Validation** - Tokens will be properly validated
? **SignalR Authentication** - SignalR connections can use JWT tokens via query parameter
? **Token Lifetime** - Expired tokens will be rejected
? **Issuer & Audience Validation** - Only tokens from ChatBlitz will be accepted
? **Swagger Authorization** - You can enter your JWT token in Swagger UI

## After Making Changes

1. **Save Program.cs**
2. **Build the project**: `dotnet build` or `Ctrl+Shift+B`
3. **Run the app**: `dotnet run` or `F5`
4. **Test in Swagger**:
   - Register a user: `POST /api/auth/register`
   - Login: `POST /api/auth/login`
   - Copy the `token` from the response
   - Click the green "Authorize" button in Swagger
   - Enter: `Bearer <paste-your-token>`
   - Click "Authorize"
   - Now you can access protected endpoints!

## Verification

After the update, you should see in Swagger:
- ?? A green "Authorize" button at the top
- ?? Lock icons next to protected endpoints
- ? No 401 Unauthorized errors when you're authenticated

Happy coding! ??
