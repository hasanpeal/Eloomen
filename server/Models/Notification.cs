using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("Notifications")]
public class Notification
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // e.g., "PasswordChanged", "VaultReleased", "ItemEdited", etc.
    
    public bool IsRead { get; set; } = false;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReadAt { get; set; }
    
    // Optional reference to related entity
    public int? VaultId { get; set; }
    public int? ItemId { get; set; }
    public int? InviteId { get; set; }
}

