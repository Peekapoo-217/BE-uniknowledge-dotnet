namespace UniKnowledge.Settings;

public class PasswordResetSettings
{
    public int OtpExpirationMinutes { get; set; } = 5;
    public int OtpLength { get; set; } = 6;
}
