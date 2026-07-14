using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class UserAddress : BaseEntity
{
    [Key]
    public int AddressId { get; set; }

    public int UserId { get; set; }

    [Required]
    [StringLength(500)]
    public string FullAddress { get; set; } = null!;

    public bool IsDefault { get; set; } = false;

    [ForeignKey("UserId")]
    public virtual Account Account { get; set; } = null!;
}
