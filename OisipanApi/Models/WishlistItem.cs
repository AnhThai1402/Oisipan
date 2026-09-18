using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models;

public class WishlistItem : BaseEntity
{
    [Key]
    public Guid WishlistItemId { get; set; }

    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual Account? Account { get; set; }

    public Guid ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }
}