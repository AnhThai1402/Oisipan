using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class NewsArticleRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn.")]
    [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự.")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
    public string Content { get; set; } = string.Empty;

    public string? Image { get; set; }

    public bool IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }
}
