using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class OrderStatusHistory : BaseEntity
{
    [Key]
    public Guid OrderStatusHistoryId { get; set; }

    public Guid OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public virtual Order? Order { get; set; }

    [StringLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string? FromStatus { get; set; }

    [Required]
    [StringLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string ToStatus { get; set; } = string.Empty;

    [StringLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string ChangedBy { get; set; } = "System";

    [StringLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Note { get; set; }
}