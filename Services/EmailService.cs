using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using UniKnowledge.Settings;

namespace UniKnowledge.Services;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otpCode);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendOtpEmailAsync(string toEmail, string otpCode)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Password Reset - OTP Code";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <html><body style='font-family:Arial'>
                    <div style='max-width:600px;margin:auto;padding:20px'>
                      <h1 style='color:#667eea'>UniKnowledge - Password Reset</h1>
                      <p>Your OTP code is:</p>
                      <div style='background:#f0f0f0;padding:20px;text-align:center;font-size:32px;letter-spacing:5px;color:#667eea;font-weight:bold'>
                        {otpCode}
                      </div>
                      <p>This code expires in 5 minutes.</p>
                    </div>
                    </body></html>
                "
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, false);
            await client.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send OTP email: {ex.Message}");
            throw;
        }
    }
}