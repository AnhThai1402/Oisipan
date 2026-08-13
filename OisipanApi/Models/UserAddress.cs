using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class UserAddress : BaseEntity
{
    [Key]
    public Guid AddressId { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [StringLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string RecipientName { get; set; } = string.Empty;

    [Required]
    [StringLength(15)]
    [Column(TypeName = "varchar(15)")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string FullAddress { get; set; } = null!;

    public bool IsDefault { get; set; } = false;

    [ForeignKey("UserId")]
    public virtual Account Account { get; set; } = null!;
}
