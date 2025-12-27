namespace server.Interfaces;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string email, string username, string confirmationCode, string confirmationUrl);
    Task SendDeviceVerificationAsync(string email, string username, string deviceCode, string verificationUrl);
    Task SendPasswordResetAsync(string email, string username, string resetCode, string resetUrl);
}

