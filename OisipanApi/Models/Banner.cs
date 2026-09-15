using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class Banner : BaseEntity
{
    [Key]
    public Guid BannerId { get; set; }

    [Required]
    [StringLength(250)]
    [Column(TypeName = "nvarchar(250)")]
    public String Title { get; set; } = String.Empty;

    [Column(TypeName = "varchar(max)")]
    public String? ImageUrl { get; set; }

    [StringLength(500)]
    [Column(TypeName = "varchar(500)")]
    public String? Link { get; set; }

    public bool IsActive { get; set; } = true;

    public short DisplayOrder { get; set; } = 0;
}
