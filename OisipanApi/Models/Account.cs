using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Oishipan.Models
{
    public class Account : BaseEntity
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string FullName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string Email { get; set; } = null!;

        [StringLength(15)]
        [Column(TypeName = "varchar(15)")]
        public string? PhoneNumber { get; set; }

        [Column(TypeName = "varchar(max)")]
        public string? Password { get; set; }

        [StringLength(100)]
        public string? GoogleId { get; set; }

        [Required]
        [StringLength(20)]
        public string AuthProvider { get; set; } = "Local";

        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Role { get; set; } = "User";

        [Column(TypeName = "nvarchar(max)")]
        public string? Address { get; set; }

        [StringLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? AvatarUrl { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
    }
}
