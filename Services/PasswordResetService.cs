using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniKnowledge.Data;
using UniKnowledge.Models;
using UniKnowledge.Settings;

namespace UniKnowledge.Services;

public interface IPasswordResetService
{
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> VerifyOtpAsync(string email, string otpCode);
    Task<bool> ResetPasswordAsync(string email, string otpCode, string newPassword);
}

public class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly PasswordResetSettings _settings;

    public PasswordResetService(
        AppDbContext context, 
        IEmailService emailService,
        IOptions<PasswordResetSettings> settings)
    {
        _context = context;
        _emailService = emailService;
        _settings = settings.Value;
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return false;

        // Generate OTP based on configured length
        var min = (int)Math.Pow(10, _settings.OtpLength - 1);
        var max = (int)Math.Pow(10, _settings.OtpLength) - 1;
        var otpPlain = new Random().Next(min, max).ToString();
        var otpHashed = BCrypt.Net.BCrypt.HashPassword(otpPlain);

        var oldTokens = await _context.PasswordResetTokens
            .Where(t => t.Email == email && !t.IsUsed)
            .ToListAsync();
        _context.PasswordResetTokens.RemoveRange(oldTokens);

        var token = new PasswordResetToken
        {
            UserId = user.UserId,
            Email = email,
            OtpCode = otpHashed,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.OtpExpirationMinutes),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();
        await _emailService.SendOtpEmailAsync(email, otpPlain);

        return true;
    }

    public async Task<bool> VerifyOtpAsync(string email, string otpCode)
    {
        var token = await _context.PasswordResetTokens
            .Where(t => t.Email == email && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        return token != null && BCrypt.Net.BCrypt.Verify(otpCode, token.OtpCode);
    }

    public async Task<bool> ResetPasswordAsync(string email, string otpCode, string newPassword)
    {
        var token = await _context.PasswordResetTokens
            .Where(t => t.Email == email && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (token == null || !BCrypt.Net.BCrypt.Verify(otpCode, token.OtpCode))
            return false;

        var user = await _context.Users.FindAsync(token.UserId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        token.IsUsed = true;

        await _context.SaveChangesAsync();
        return true;
    }
}