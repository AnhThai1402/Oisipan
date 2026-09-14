using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class GoogleLoginRequest
{
    [Required(ErrorMessage = "Google ID Token là bắt buộc.")]
    public string IdToken { get; set; } = string.Empty;
}
