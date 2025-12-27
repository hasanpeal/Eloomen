using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VerificationCodes")]
public class VerificationCode
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; }
    [ForeignKey("UserId")]
    public User User { get; set; }
    [Required]
    public string CodeHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}