namespace Oishipan.DTOs;

public class SendInvoiceRequest
{
    public string Email { get; set; } = string.Empty;
    public bool AttachPdf { get; set; } = true;
    public bool SaveCopy { get; set; } = true;
    public string? Message { get; set; }
}
