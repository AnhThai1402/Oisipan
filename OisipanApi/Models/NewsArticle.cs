using System.ComponentModel.DataAnnotations;

namespace Oishipan.Models;

public class NewsArticle : BaseEntity
{
    [Key]
    public int NewsArticleId { get; set; }

    [Required]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? Image { get; set; }

    public bool IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }
}
