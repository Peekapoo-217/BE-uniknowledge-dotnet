# Email Configuration Setup

## Required: .env File

Create a `.env` file in this directory (BE/UniKnowledge/) with your Gmail credentials:

```env
EMAIL_SENDER_EMAIL=your-email@gmail.com
EMAIL_SENDER_PASSWORD=your-16-char-app-password
```

## How to Get Gmail App Password

1. Go to https://myaccount.google.com/apppasswords
2. Enable 2-Factor Authentication if not already enabled
3. Create App Password:
   - Select app: Other (Custom name)
   - Name it: UniKnowledge Backend
   - Click Generate
4. Copy the 16-character password (remove spaces)
5. Paste into .env file

## Important Notes

- The .env file is gitignored and will not be committed
- Use App Password, NOT your regular Gmail password
- Do not share or commit this file
- For production, use server environment variables

## Configuration

Edit `appsettings.json` to customize OTP settings:

```json
{
  "PasswordResetSettings": {
    "OtpExpirationMinutes": 5,
    "OtpLength": 6
  }
}
```

## Testing

After creating .env file:

1. Restart backend: `dotnet run`
2. Test forgot-password endpoint:
   ```
   POST http://localhost:5134/api/auth/forgot-password
   { "email": "registered-email@gmail.com" }
   ```
3. Check email inbox for OTP code
