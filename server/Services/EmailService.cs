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
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>Welcome to Eloomen!</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Thank you for signing up. Please verify your email address using the verification code below:</p>
                    
                    <div style='background-color: #0f172a; padding: 24px; border-radius: 8px; margin: 24px 0; text-align: center; border: 1px solid #334155;'>
                        <p style='margin: 0 0 12px 0; color: #94a3b8; font-size: 14px; text-transform: uppercase; letter-spacing: 1px;'>Verification Code</p>
                        <h1 style='font-size: 36px; letter-spacing: 8px; color: #10b981; margin: 0; font-weight: 700; font-family: ""Courier New"", monospace;'>{confirmationCode}</h1>
                    </div>
                    
                    <div style='text-align: center; margin: 32px 0;'>
                        <a href='{fullUrl}' style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: 600; font-size: 16px; box-shadow: 0 4px 6px rgba(16, 185, 129, 0.3);'>Verify Email</a>
                    </div>
                    
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This code will expire in 24 hours. If you didn't create an account, please ignore this email.
                    </p>
                </div>
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
            throw new InvalidOperationException("Failed to send email confirmation");
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
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>New Device Detected</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>We detected a login attempt from a new device. Please verify this device using the verification code below:</p>
                    
                    <div style='background-color: #0f172a; padding: 24px; border-radius: 8px; margin: 24px 0; text-align: center; border: 1px solid #334155;'>
                        <p style='margin: 0 0 12px 0; color: #94a3b8; font-size: 14px; text-transform: uppercase; letter-spacing: 1px;'>Device Verification Code</p>
                        <h1 style='font-size: 36px; letter-spacing: 8px; color: #3b82f6; margin: 0; font-weight: 700; font-family: ""Courier New"", monospace;'>{deviceCode}</h1>
                    </div>
                    
                    
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This code will expire in 1 hour. If you didn't attempt to log in, please secure your account immediately.
                    </p>
                </div>
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
            throw new InvalidOperationException("Failed to send device verification email");
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
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>Password Reset Request</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>You requested to reset your password. Please use the verification code below:</p>
                    
                    <div style='background-color: #0f172a; padding: 24px; border-radius: 8px; margin: 24px 0; text-align: center; border: 1px solid #334155;'>
                        <p style='margin: 0 0 12px 0; color: #94a3b8; font-size: 14px; text-transform: uppercase; letter-spacing: 1px;'>Reset Code</p>
                        <h1 style='font-size: 36px; letter-spacing: 8px; color: #f59e0b; margin: 0; font-weight: 700; font-family: ""Courier New"", monospace;'>{resetCode}</h1>
                    </div>
                    
                    <div style='text-align: center; margin: 32px 0;'>
                        <a href='{fullUrl}' style='background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: 600; font-size: 16px; box-shadow: 0 4px 6px rgba(245, 158, 11, 0.3);'>Reset Password</a>
                    </div>
                    
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This code will expire in 1 hour. If you didn't request a password reset, please ignore this email and your password will remain unchanged.
                    </p>
                </div>
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
            throw new InvalidOperationException("Failed to send password reset email");
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
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>Vault Invitation</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi there,</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'><strong style='color: #f1f5f9;'>{inviterName}</strong> has invited you to join the vault <strong style='color: #818cf8;'>{vaultName}</strong>.</p>
                    
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; text-align: center; border: 1px solid #334155;'>
                        <p style='margin: 0 0 12px 0; color: #94a3b8; font-size: 14px; text-transform: uppercase; letter-spacing: 1px;'>Your Privilege Level</p>
                        <div style='display: inline-block;'>{privilegeBadge}</div>
                    </div>
                    
                    {noteSection}
                    
                    <div style='text-align: center; margin: 32px 0;'>
                        <a href='{inviteUrl}' style='background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%); color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: 600; font-size: 16px; box-shadow: 0 4px 6px rgba(99, 102, 241, 0.3);'>Accept Invitation</a>
                    </div>
                    
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
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
            throw new InvalidOperationException("Failed to send vault invite email");
        }
    }

    public async Task SendContactEmailAsync(string userName, string userEmail, string userId, string contactName, string message)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        var fromEmail = _config["SendGrid:FromEmail"];
        var fromName = _config["SendGrid:FromName"] ?? "Eloomen";
        var adminEmail = _config["SendGrid:AdminEmail"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
        {
            throw new InvalidOperationException("SendGrid configuration is missing");
        }

        if (string.IsNullOrEmpty(adminEmail))
        {
            throw new InvalidOperationException("SendGrid:AdminEmail is not configured");
        }

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(fromEmail, fromName);
        var to = new EmailAddress(adminEmail);
        var subject = $"Contact Form Submission from {contactName}";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>New Contact Form Submission</h2>
                    
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155;'>
                        <h3 style='color: #cbd5e1; font-size: 16px; margin-top: 0; margin-bottom: 16px; font-weight: 600;'>User Information</h3>
                        <p style='color: #94a3b8; font-size: 14px; margin: 8px 0;'><strong style='color: #cbd5e1;'>User ID:</strong> {userId}</p>
                        <p style='color: #94a3b8; font-size: 14px; margin: 8px 0;'><strong style='color: #cbd5e1;'>Username:</strong> {userName}</p>
                        <p style='color: #94a3b8; font-size: 14px; margin: 8px 0;'><strong style='color: #cbd5e1;'>Email:</strong> {userEmail}</p>
                        <p style='color: #94a3b8; font-size: 14px; margin: 8px 0;'><strong style='color: #cbd5e1;'>Contact Name:</strong> {contactName}</p>
                    </div>

                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155;'>
                        <h3 style='color: #cbd5e1; font-size: 16px; margin-top: 0; margin-bottom: 16px; font-weight: 600;'>Message</h3>
                        <p style='color: #e2e8f0; font-size: 15px; line-height: 1.6; margin: 0; white-space: pre-wrap;'>{message}</p>
                    </div>

                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This message was sent from the Eloomen contact form.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            New Contact Form Submission

            User Information:
            User ID: {userId}
            Username: {userName}
            Email: {userEmail}
            Contact Name: {contactName}

            Message:
            {message}

            This message was sent from the Eloomen contact form.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send contact email");
        }
    }

    public async Task SendPasswordChangedConfirmationAsync(string email, string username)
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
        var subject = "Password Changed Successfully";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>Password Changed</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Your password has been successfully changed.</p>
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155;'>
                        <p style='color: #94a3b8; font-size: 14px; margin: 0;'>If you did not make this change, please secure your account immediately by resetting your password.</p>
                    </div>
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This is an automated notification from Eloomen.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            Password Changed

            Hi {username},

            Your password has been successfully changed.

            If you did not make this change, please secure your account immediately by resetting your password.

            This is an automated notification from Eloomen.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send password changed confirmation email");
        }
    }

    public async Task SendVaultReleasedNotificationAsync(string email, string username, string vaultName)
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
        var subject = $"Vault Released: {vaultName}";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>Vault Released</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>The vault <strong style='color: #818cf8;'>{vaultName}</strong> has been released and is now accessible.</p>
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155; text-align: center;'>
                        <p style='color: #10b981; font-size: 18px; font-weight: 600; margin: 0;'>✓ Vault is now accessible</p>
                    </div>
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        You can now access all items in this vault.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            Vault Released

            Hi {username},

            The vault {vaultName} has been released and is now accessible.

            You can now access all items in this vault.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send vault released notification");
        }
    }

    public async Task SendVaultItemChangedNotificationAsync(string email, string username, string vaultName, string itemTitle, string action, string editorName)
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
        var subject = $"Item {action} in Vault: {vaultName}";

        var actionColor = action == "edited" ? "#3b82f6" : "#ef4444";
        var actionIcon = action == "edited" ? "✏️" : "🗑️";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>{actionIcon} Item {action.ToUpper()}</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'><strong style='color: #f1f5f9;'>{editorName}</strong> has {action} the item <strong style='color: #818cf8;'>{itemTitle}</strong> in the vault <strong style='color: #818cf8;'>{vaultName}</strong>.</p>
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border-left: 4px solid {actionColor};'>
                        <p style='color: #cbd5e1; font-size: 14px; margin: 0;'><strong>Vault:</strong> {vaultName}</p>
                        <p style='color: #cbd5e1; font-size: 14px; margin: 8px 0 0 0;'><strong>Item:</strong> {itemTitle}</p>
                        <p style='color: #cbd5e1; font-size: 14px; margin: 8px 0 0 0;'><strong>Action:</strong> {action}</p>
                        <p style='color: #cbd5e1; font-size: 14px; margin: 8px 0 0 0;'><strong>By:</strong> {editorName}</p>
                    </div>
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This is an automated notification from Eloomen.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            Item {action.ToUpper()}

            Hi {username},

            {editorName} has {action} the item {itemTitle} in the vault {vaultName}.

            Vault: {vaultName}
            Item: {itemTitle}
            Action: {action}
            By: {editorName}

            This is an automated notification from Eloomen.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send vault item changed notification");
        }
    }

    public async Task SendVaultPolicyChangedNotificationAsync(string email, string username, string vaultName, string policyType)
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
        var subject = $"Vault Policy Changed: {vaultName}";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>Vault Policy Changed</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>The policy for the vault <strong style='color: #818cf8;'>{vaultName}</strong> has been changed to <strong style='color: #f1f5f9;'>{policyType}</strong>.</p>
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155;'>
                        <p style='color: #cbd5e1; font-size: 14px; margin: 0;'><strong>Vault:</strong> {vaultName}</p>
                        <p style='color: #cbd5e1; font-size: 14px; margin: 8px 0 0 0;'><strong>New Policy:</strong> {policyType}</p>
                    </div>
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This is an automated notification from Eloomen.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            Vault Policy Changed

            Hi {username},

            The policy for the vault {vaultName} has been changed to {policyType}.

            Vault: {vaultName}
            New Policy: {policyType}

            This is an automated notification from Eloomen.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send vault policy changed notification");
        }
    }

    public async Task SendVaultDeletedNotificationAsync(string email, string username, string vaultName)
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
        var subject = $"Vault Deleted: {vaultName}";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>Vault Deleted</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>The vault <strong style='color: #818cf8;'>{vaultName}</strong> has been deleted.</p>
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155;'>
                        <p style='color: #ef4444; font-size: 14px; margin: 0;'>⚠️ All items and data in this vault have been permanently deleted.</p>
                    </div>
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This is an automated notification from Eloomen.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            Vault Deleted

            Hi {username},

            The vault {vaultName} has been deleted.

            ⚠️ All items and data in this vault have been permanently deleted.

            This is an automated notification from Eloomen.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send vault deleted notification");
        }
    }

    public async Task SendInviteSentToOwnerNotificationAsync(string email, string username, string vaultName, string inviterName, string inviteeEmail)
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
        var subject = $"New Invite Sent in Vault: {vaultName}";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>New Invite Sent</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'><strong style='color: #f1f5f9;'>{inviterName}</strong> has sent an invitation to <strong style='color: #818cf8;'>{inviteeEmail}</strong> for the vault <strong style='color: #818cf8;'>{vaultName}</strong>.</p>
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155;'>
                        <p style='color: #cbd5e1; font-size: 14px; margin: 0;'><strong>Vault:</strong> {vaultName}</p>
                        <p style='color: #cbd5e1; font-size: 14px; margin: 8px 0 0 0;'><strong>Inviter:</strong> {inviterName}</p>
                        <p style='color: #cbd5e1; font-size: 14px; margin: 8px 0 0 0;'><strong>Invitee:</strong> {inviteeEmail}</p>
                    </div>
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This is an automated notification from Eloomen.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            New Invite Sent

            Hi {username},

            {inviterName} has sent an invitation to {inviteeEmail} for the vault {vaultName}.

            Vault: {vaultName}
            Inviter: {inviterName}
            Invitee: {inviteeEmail}

            This is an automated notification from Eloomen.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send invite sent notification");
        }
    }

    public async Task SendInviteAcceptedToOwnerNotificationAsync(string email, string username, string vaultName, string memberName)
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
        var subject = $"New Member Joined: {vaultName}";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>New Member Joined</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'><strong style='color: #f1f5f9;'>{memberName}</strong> has accepted the invitation and joined the vault <strong style='color: #818cf8;'>{vaultName}</strong>.</p>
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155; text-align: center;'>
                        <p style='color: #10b981; font-size: 18px; font-weight: 600; margin: 0;'>✓ New member added</p>
                    </div>
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        This is an automated notification from Eloomen.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            New Member Joined

            Hi {username},

            {memberName} has accepted the invitation and joined the vault {vaultName}.

            This is an automated notification from Eloomen.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send invite accepted notification");
        }
    }

    public async Task SendAccountDeletedConfirmationAsync(string email, string username)
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
        var subject = "Account Deleted Successfully";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>Account Deleted</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Your account has been successfully deleted.</p>
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155;'>
                        <p style='color: #94a3b8; font-size: 14px; margin: 0;'>All your data, vaults, and account information have been permanently removed from our system.</p>
                    </div>
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        If you did not request this deletion, please contact support immediately.
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            Account Deleted

            Hi {username},

            Your account has been successfully deleted.

            All your data, vaults, and account information have been permanently removed from our system.

            If you did not request this deletion, please contact support immediately.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send account deleted confirmation email");
        }
    }

    public async Task SendInviteExpiredNotificationAsync(string email, string username, string vaultName, bool isInviter)
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
        var subject = isInviter ? $"Invite Expired: {vaultName}" : $"Invitation Expired: {vaultName}";

        var htmlContent = $@"
            <html>
            <body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; padding: 20px; background-color: #0f172a; color: #e2e8f0; margin: 0;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 12px; padding: 32px; border: 1px solid #334155;'>
                    <h2 style='color: #f1f5f9; margin-top: 0; font-size: 24px; font-weight: 600;'>Invitation Expired</h2>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>Hi {username},</p>
                    <p style='color: #cbd5e1; font-size: 16px; line-height: 1.6;'>{(isInviter ? $"The invitation you sent for the vault <strong style='color: #818cf8;'>{vaultName}</strong> has expired." : $"Your invitation to join the vault <strong style='color: #818cf8;'>{vaultName}</strong> has expired.")}</p>
                    <div style='background-color: #0f172a; padding: 20px; border-radius: 8px; margin: 24px 0; border: 1px solid #334155;'>
                        <p style='color: #f59e0b; font-size: 14px; margin: 0;'>⚠️ This invitation is no longer valid.</p>
                    </div>
                    <p style='color: #94a3b8; font-size: 14px; margin-top: 32px; border-top: 1px solid #334155; padding-top: 20px; line-height: 1.6;'>
                        {(isInviter ? "You can send a new invitation if needed." : "Please contact the vault owner if you would like to join this vault.")}
                    </p>
                </div>
            </body>
            </html>";

        var plainTextContent = $@"
            Invitation Expired

            Hi {username},

            {(isInviter ? $"The invitation you sent for the vault {vaultName} has expired." : $"Your invitation to join the vault {vaultName} has expired.")}

            ⚠️ This invitation is no longer valid.

            {(isInviter ? "You can send a new invitation if needed." : "Please contact the vault owner if you would like to join this vault.")}";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to send invite expired notification");
        }
    }
}

