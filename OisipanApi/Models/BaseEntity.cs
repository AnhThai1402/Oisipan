using System.ComponentModel.DataAnnotations;

namespace Oishipan.Models
{
    public abstract class BaseEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active";
    }
}
