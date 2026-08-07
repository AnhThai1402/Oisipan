using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FrontendMvc.Models;

public class BannerViewModel
{
    public Guid BannerId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề banner.")]
    [StringLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự.")]
    [Display(Name = "Tiêu đề")]
    public String Title { get; set; } = String.Empty;

    [Display(Name = "Hình ảnh")]
    public String? ImageUrl { get; set; }

    [Display(Name = "File ảnh tải lên")]
    public IFormFile? ImageFile { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn không được vượt quá 500 ký tự.")]
    [Display(Name = "Đường dẫn (Link)")]
    public String? Link { get; set; }

    [Display(Name = "Trạng thái hiển thị")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Thứ tự hiển thị")]
    public short DisplayOrder { get; set; } = 0;
}
