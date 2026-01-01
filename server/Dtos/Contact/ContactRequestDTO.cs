using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Contact;

public class ContactRequestDTO
{
    [Required]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(5000, ErrorMessage = "Message cannot exceed 5000 characters.")]
    public string Message { get; set; } = string.Empty;
}

