namespace FrontendMvc.Models
{
    public class NumPhoneModel
    {
        public string PhoneNumber { get; set; } = null!;
        public string OTP { get; set; } = null!;
        public string? IdToken { get; set; }
    }
}
