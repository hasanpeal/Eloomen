using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultDocuments")]
public class VaultDocument
{
    [Key]
    [ForeignKey(nameof(VaultItem))]
    public int VaultItemId { get; set; }
    
    public VaultItem? VaultItem { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string ObjectKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string OriginalFileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ContentType { get; set; } = string.Empty;
    
    [Required]
    public long FileSize { get; set; }
    
    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

