using System.ComponentModel.DataAnnotations;

namespace Oishipan.Models
{
    public class Account
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        public string? Password { get; set; }

        [StringLength(100)]
        public string? GoogleId { get; set; }

        [Required]
        [StringLength(20)]
        public string AuthProvider { get; set; } = "Local";

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "User";

        public string? Address { get; set; }

        public bool Status { get; set; } = true;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}