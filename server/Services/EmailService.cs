using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using SendGrid;
using SendGrid.Helpers.Mail;
using server.Interfaces;

namespace server.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailConfirmationAsync(string email, string username, string confirmationCode, string confirmationUrl)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        var fromEmail = _config["SendGrid:FromEmail"];
        var fromName = _config["SendGrid:FromName"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
        {
            throw new InvalidOperationException("SendGrid configuration is missing");
        }

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(email, username);
        var subject = "Verify your email address";
        
        var encodedCode = Uri.EscapeDataString(confirmationCode);
        var encodedEmail = Uri.EscapeDataString(email);
        var fullUrl = $"{confirmationUrl}?code={encodedCode}&email={encodedEmail}";
        
        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Welcome to Eloomen!</h2>
                <p>Hi {username},</p>
                <p>Thank you for signing up. Please verify your email address using the verification code below:</p>
                <div style='background-color: #f0f0f0; padding: 20px; text-align: center; border-radius: 5px; margin: 20px 0;'>
                    <h1 style='font-size: 32px; letter-spacing: 5px; color: #4CAF50; margin: 0;'>{confirmationCode}</h1>
                </div>
                <p>Or click the link below to verify automatically:</p>
                <p><a href='{fullUrl}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Verify Email</a></p>
                <p>This code will expire in 24 hours.</p>
                <p>If you didn't create an account, please ignore this email.</p>
            </body>
            </html>";

        var plainTextContent = $@"
            Welcome to Eloomen!
            
            Hi {username},
            
            Thank you for signing up. Please verify your email address using the verification code:
            
            {confirmationCode}
            
            Or visit this link to verify automatically:
            {fullUrl}
            
            This code will expire in 24 hours.
            
            If you didn't create an account, please ignore this email.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to send email confirmation. Status: {response.StatusCode}, Body: {body}");
        }
    }

    public async Task SendDeviceVerificationAsync(string email, string username, string deviceCode, string verificationUrl)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        var fromEmail = _config["SendGrid:FromEmail"];
        var fromName = _config["SendGrid:FromName"] ?? "Eloomen";

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
        {
            throw new InvalidOperationException("SendGrid configuration is missing");
        }

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(email, username);
        var subject = "Verify your new device";
        
        var encodedCode = Uri.EscapeDataString(deviceCode);
        var fullUrl = $"{verificationUrl}?code={encodedCode}";
        
        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>New Device Detected</h2>
                <p>Hi {username},</p>
                <p>We detected a login attempt from a new device. Please verify this device using the verification code below:</p>
                <div style='background-color: #f0f0f0; padding: 20px; text-align: center; border-radius: 5px; margin: 20px 0;'>
                    <h1 style='font-size: 32px; letter-spacing: 5px; color: #2196F3; margin: 0;'>{deviceCode}</h1>
                </div>
                <p>Or click the link below to verify automatically:</p>
                <p><a href='{fullUrl}' style='background-color: #2196F3; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Verify Device</a></p>
                <p>This code will expire in 1 hour.</p>
                <p>If you didn't attempt to log in, please secure your account immediately.</p>
            </body>
            </html>";

        var plainTextContent = $@"
            New Device Detected
            
            Hi {username},
            
            We detected a login attempt from a new device. Please verify this device using the verification code:
            
            {deviceCode}
            
            Or visit this link to verify automatically:
            {fullUrl}
            
            This code will expire in 1 hour.
            
            If you didn't attempt to log in, please secure your account immediately.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to send device verification email. Status: {response.StatusCode}, Body: {body}");
        }
    }

    public async Task SendPasswordResetAsync(string email, string username, string resetCode, string resetUrl)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        var fromEmail = _config["SendGrid:FromEmail"];
        var fromName = _config["SendGrid:FromName"] ?? "Eloomen";

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
        {
            throw new InvalidOperationException("SendGrid configuration is missing");
        }

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(email, username);
        var subject = "Reset your password";
        
        var encodedCode = Uri.EscapeDataString(resetCode);
        var encodedEmail = Uri.EscapeDataString(email);
        var fullUrl = $"{resetUrl}?code={encodedCode}&email={encodedEmail}";
        
        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Password Reset Request</h2>
                <p>Hi {username},</p>
                <p>You requested to reset your password. Please use the verification code below:</p>
                <div style='background-color: #f0f0f0; padding: 20px; text-align: center; border-radius: 5px; margin: 20px 0;'>
                    <h1 style='font-size: 32px; letter-spacing: 5px; color: #FF9800; margin: 0;'>{resetCode}</h1>
                </div>
                <p>Or click the link below to reset your password automatically:</p>
                <p><a href='{fullUrl}' style='background-color: #FF9800; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a></p>
                <p>This code will expire in 1 hour.</p>
                <p>If you didn't request a password reset, please ignore this email and your password will remain unchanged.</p>
            </body>
            </html>";

        var plainTextContent = $@"
            Password Reset Request
            
            Hi {username},
            
            You requested to reset your password. Please use the verification code:
            
            {resetCode}
            
            Or visit this link to reset automatically:
            {fullUrl}
            
            This code will expire in 1 hour.
            
            If you didn't request a password reset, please ignore this email and your password will remain unchanged.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to send password reset email. Status: {response.StatusCode}, Body: {body}");
        }
    }
}

