using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class InvoiceRecord : BaseEntity
{
    [Key]
    public int InvoiceRecordId { get; set; }
    public int OrderId { get; set; }
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
}
