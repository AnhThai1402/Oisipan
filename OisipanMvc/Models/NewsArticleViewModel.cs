using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models;

public class NewsArticleViewModel
{
    public int NewsArticleId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự.")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn.")]
    [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự.")]
    [Display(Name = "Mô tả ngắn")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
    [Display(Name = "Nội dung")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Ảnh đại diện")]
    public string? Image { get; set; }

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Xuất bản")]
    public bool IsPublished { get; set; }

    [Display(Name = "Ngày xuất bản")]
    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
