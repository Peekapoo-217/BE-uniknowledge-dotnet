using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UniKnowledge.Data;
using UniKnowledge.Hubs;
using UniKnowledge.Services;
using UniKnowledge.Settings;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                    else
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Request.Path.Value?.Contains("/hubs") == true)
                {
                    context.NoResult();
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add SignalR with error handling
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Only in development
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB max message size
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IAnswerService, AnswerService>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IMessageService, MessageService>();

// thêm Services UserProfile
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// Configure EmailSettings (merge appsettings.json + environment variables)
builder.Services.Configure<EmailSettings>(options =>
{
    builder.Configuration.GetSection("EmailSettings").Bind(options);
    // Override with environment variables if present
    var envEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL");
    var envPassword = Environment.GetEnvironmentVariable("EMAIL_SENDER_PASSWORD");
    if (!string.IsNullOrEmpty(envEmail)) options.SenderEmail = envEmail;
    if (!string.IsNullOrEmpty(envPassword)) options.SenderPassword = envPassword;
});

// Configure PasswordResetSettings from appsettings.json
builder.Services.Configure<PasswordResetSettings>(
    builder.Configuration.GetSection("PasswordResetSettings"));

// thêm services password
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable static files serving (for uploaded files)
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
