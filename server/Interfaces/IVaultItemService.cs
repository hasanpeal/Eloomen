using server.Dtos.VaultItem;
using server.Models;

namespace server.Interfaces;

public interface IVaultItemService
{
    Task<VaultItemResponseDTO?> GetItemByIdAsync(int itemId, string userId);
    Task<List<VaultItemResponseDTO>> GetVaultItemsAsync(int vaultId, string userId);
    Task<VaultItemResponseDTO> CreateItemAsync(CreateVaultItemDTO dto, string userId);
    Task<VaultItemResponseDTO?> UpdateItemAsync(int itemId, UpdateVaultItemDTO dto, string userId);
    Task<bool> DeleteItemAsync(int itemId, string userId);
    Task<bool> RestoreItemAsync(int itemId, string userId);
    Task<ItemPermission?> GetUserPermissionAsync(int itemId, string userId);
    Task<bool> CanViewItemAsync(int itemId, string userId);
    Task<bool> CanEditItemAsync(int itemId, string userId);
}

