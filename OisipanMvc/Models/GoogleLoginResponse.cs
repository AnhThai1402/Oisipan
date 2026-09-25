namespace FrontendMvc.Models;

public class GoogleLoginResponse
{
    public bool RequiresPasswordSetup { get; set; }
    public string? SetupEmail { get; set; }
    public string? SetupFullName { get; set; }
    public AuthResponse? Auth { get; set; }
}
