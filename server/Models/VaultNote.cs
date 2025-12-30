using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultNotes")]
public class VaultNote
{
    [Key]
    [ForeignKey(nameof(VaultItem))]
    public int VaultItemId { get; set; }
    
    public VaultItem? VaultItem { get; set; }
    
    [Required]
    public string EncryptedContent { get; set; } = string.Empty;
    
    [Required]
    public ContentFormat ContentFormat { get; set; } = ContentFormat.PlainText;
}

