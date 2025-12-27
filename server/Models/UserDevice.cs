using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("UserDevices")]
public class UserDevice
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    
    [Required]
    public string DeviceIdentifier { get; set; } = string.Empty;
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime? VerifiedAt { get; set; }
    
    // Same device can have multiple refresh tokens (Different browsers + We want to track old tokens)
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}