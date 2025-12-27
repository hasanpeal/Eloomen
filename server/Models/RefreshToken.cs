using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("RefreshTokens")]
public class RefreshToken
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Token { get; set; }
    [Required]
    public int UserDeviceId { get; set; }
    [ForeignKey(nameof(UserDeviceId))]
    public UserDevice UserDevice { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}