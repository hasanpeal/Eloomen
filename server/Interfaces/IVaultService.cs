using server.Dtos.Vault;
using server.Models;

namespace server.Interfaces;

public interface IVaultService
{
    Task<VaultResponseDTO?> GetVaultByIdAsync(int vaultId, string userId);
    Task<List<VaultResponseDTO>> GetUserVaultsAsync(string userId);
    Task<VaultResponseDTO> CreateVaultAsync(CreateVaultDTO dto, string userId);
    Task<VaultResponseDTO?> UpdateVaultAsync(int vaultId, UpdateVaultDTO dto, string userId);
    Task<bool> DeleteVaultAsync(int vaultId, string userId);
    Task<bool> RestoreVaultAsync(int vaultId, string userId);
    
    // Invites
    Task<VaultInviteResponseDTO> CreateInviteAsync(int vaultId, CreateInviteDTO dto, string inviterId);
    Task<List<VaultInviteResponseDTO>> GetVaultInvitesAsync(int vaultId, string userId);
    Task<bool> CancelInviteAsync(int inviteId, string userId);
    Task<bool> ResendInviteAsync(int inviteId, string userId);
    Task<bool> AcceptInviteAsync(string token, string email, string userId);
    Task<InviteInfoDTO> GetInviteInfoAsync(string token);
    
    // Members
    Task<List<VaultMemberResponseDTO>> GetVaultMembersAsync(int vaultId, string userId);
    Task<bool> RemoveMemberAsync(int vaultId, int memberId, string userId);
    Task<bool> UpdateMemberPrivilegeAsync(int vaultId, UpdateMemberPrivilegeDTO dto, string userId);
    Task<bool> TransferOwnershipAsync(int vaultId, TransferOwnershipDTO dto, string userId);
    Task<bool> LeaveVaultAsync(int vaultId, string userId);
    
    // Policy operations
    Task<bool> ReleaseVaultManuallyAsync(int vaultId, string userId);
    
    // Permission checks
    Task<Privilege?> GetUserPrivilegeAsync(int vaultId, string userId);
    Task<bool> CanViewVaultAsync(int vaultId, string userId);
    Task<bool> CanEditVaultAsync(int vaultId, string userId);
    Task<bool> CanManageMembersAsync(int vaultId, string userId);
    Task<bool> CanDeleteVaultAsync(int vaultId, string userId);
    Task<bool> IsVaultAccessibleAsync(int vaultId, string userId);
    
    // Logs
    Task<List<VaultLogResponseDTO>> GetVaultLogsAsync(int vaultId, string userId);
}

