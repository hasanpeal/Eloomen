using System.ComponentModel.DataAnnotations;
using server.Models;

namespace server.Dtos.Vault;

public class UpdateMemberPrivilegeDTO
{
    [Required]
    public int MemberId { get; set; }
    
    [Required]
    public Privilege Privilege { get; set; }
}

