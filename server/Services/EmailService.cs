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

    public async Task SendVaultInviteAsync(string email, string inviterName, string vaultName, string inviteUrl, string privilege, string? note = null)
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
        var to = new EmailAddress(email);
        var subject = $"You've been invited to join the vault: {vaultName}";
        
        var privilegeBadge = privilege switch
        {
            "Owner" => "<span style='background-color: #FFD700; color: #000; padding: 4px 12px; border-radius: 12px; font-weight: bold; font-size: 12px;'>👑 OWNER</span>",
            "Admin" => "<span style='background-color: #2196F3; color: #fff; padding: 4px 12px; border-radius: 12px; font-weight: bold; font-size: 12px;'>🛠 ADMIN</span>",
            "Member" => "<span style='background-color: #4CAF50; color: #fff; padding: 4px 12px; border-radius: 12px; font-weight: bold; font-size: 12px;'>👀 MEMBER</span>",
            _ => $"<span style='background-color: #757575; color: #fff; padding: 4px 12px; border-radius: 12px; font-weight: bold; font-size: 12px;'>{privilege}</span>"
        };

        var noteSection = !string.IsNullOrEmpty(note) 
            ? $@"
                <div style='background-color: #1e293b; padding: 16px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #6366f1;'>
                    <p style='margin: 0; color: #cbd5e1; font-style: italic;'>&quot;{note}&quot;</p>
                </div>"
            : "";

        var htmlContent = $@"
            <html>
            <body style='font-family: Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0;'>Vault Invitation</h2>
                    <p style='color: #cbd5e1; font-size: 16px;'>Hi there,</p>
                    <p style='color: #cbd5e1; font-size: 16px;'><strong style='color: #f1f5f9;'>{inviterName}</strong> has invited you to join the vault <strong style='color: #818cf8;'>{vaultName}</strong>.</p>
                    
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; text-align: center; border: 1px solid #334155;'>
                        <p style='margin: 0 0 12px 0; color: #94a3b8; font-size: 14px;'>Your Privilege Level</p>
                        <div style='display: inline-block;'>{privilegeBadge}</div>
                    </div>
                    
                    {noteSection}
                    
                    <div style='text-align: center; margin: 32px 0;'>
                        <a href='{inviteUrl}' style='background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: 600; font-size: 16px; box-shadow: 0 4px 6px rgba(99, 102, 241, 0.3);'>Accept Invitation</a>
                    </div>
                    
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px;'>
                        This invitation will expire in 7 days. If you didn't expect this invitation, you can safely ignore this email.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            Vault Invitation
            
            Hi there,
            
            {inviterName} has invited you to join the vault: {vaultName}
            
            Your Privilege Level: {privilege}
            
            {(string.IsNullOrEmpty(note) ? "" : $"Note: {note}\n\n")}
            Accept your invitation by visiting this link:
            {inviteUrl}
            
            This invitation will expire in 7 days. If you didn't expect this invitation, you can safely ignore this email.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to send vault invite email. Status: {response.StatusCode}, Body: {body}");
        }
    }
}

