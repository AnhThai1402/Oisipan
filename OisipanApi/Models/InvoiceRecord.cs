using System.ComponentModel.DataAnnotations;

namespace Oishipan.Models;

public class InvoiceRecord
{
    [Key]
    public int InvoiceRecordId { get; set; }
    public int OrderId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public bool EmailSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
}
