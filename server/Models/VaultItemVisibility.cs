using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultItemVisibilities")]
public class VaultItemVisibility
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int VaultItemId { get; set; }
    
    [ForeignKey(nameof(VaultItemId))]
    public VaultItem? VaultItem { get; set; }
    
    [Required]
    public int VaultMemberId { get; set; }
    
    [ForeignKey(nameof(VaultMemberId))]
    public VaultMember? VaultMember { get; set; }
    
    [Required]
    public ItemPermission Permission { get; set; }
}

