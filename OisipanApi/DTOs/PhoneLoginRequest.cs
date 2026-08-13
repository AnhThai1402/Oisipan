using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class PhoneLoginRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
