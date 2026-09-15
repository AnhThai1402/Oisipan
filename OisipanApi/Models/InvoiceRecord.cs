using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class InvoiceRecord : BaseEntity
{
    [Key]
    public Guid InvoiceRecordId { get; set; }
    public Guid OrderId { get; set; }
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }
    [Column(TypeName = "varchar(max)")]
    public string FilePath { get; set; } = string.Empty;
    [Column(TypeName = "varchar(100)")]
    public string? Email { get; set; }
    public bool EmailSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
}
