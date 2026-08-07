using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class UserAddress : BaseEntity
{
    [Key]
    public Guid AddressId { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [StringLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string FullAddress { get; set; } = null!;

    public bool IsDefault { get; set; } = false;

    [ForeignKey("UserId")]
    public virtual Account Account { get; set; } = null!;
}
