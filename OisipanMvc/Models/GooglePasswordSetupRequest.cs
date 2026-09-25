namespace FrontendMvc.Models;

public class GooglePasswordSetupRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
