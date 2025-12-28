using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Vault;

public class TransferOwnershipDTO
{
    [Required]
    public int MemberId { get; set; }
}

