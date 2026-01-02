namespace server.Interfaces;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string email, string username, string confirmationCode, string confirmationUrl);
    Task SendDeviceVerificationAsync(string email, string username, string deviceCode, string verificationUrl);
    Task SendPasswordResetAsync(string email, string username, string resetCode, string resetUrl);
    Task SendVaultInviteAsync(string email, string inviterName, string vaultName, string inviteUrl, string privilege, string? note = null);
    Task SendContactEmailAsync(string userName, string userEmail, string userId, string contactName, string message);
    Task SendPasswordChangedConfirmationAsync(string email, string username);
    Task SendVaultReleasedNotificationAsync(string email, string username, string vaultName);
    Task SendVaultItemChangedNotificationAsync(string email, string username, string vaultName, string itemTitle, string action, string editorName);
    Task SendVaultPolicyChangedNotificationAsync(string email, string username, string vaultName, string policyType);
    Task SendVaultDeletedNotificationAsync(string email, string username, string vaultName);
    Task SendInviteSentToOwnerNotificationAsync(string email, string username, string vaultName, string inviterName, string inviteeEmail);
    Task SendInviteAcceptedToOwnerNotificationAsync(string email, string username, string vaultName, string memberName);
    Task SendAccountDeletedConfirmationAsync(string email, string username);
    Task SendInviteExpiredNotificationAsync(string email, string username, string vaultName, bool isInviter);
}

