using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FrontendMvc.Models;

public class UserAdminViewModel
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [RegularExpression("^User$", ErrorMessage = "Không thể cấp quyền quản trị viên cho tài khoản khác.")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "User";

    [Display(Name = "Đang hoạt động")]
    public bool Status { get; set; } = true;

    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [Display(Name = "Mật khẩu")]
    public string? Password { get; set; }

    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    [Display(Name = "Mật khẩu mới")]
    public string? NewPassword { get; set; }

    public int OrderCount { get; set; }

    public List<SelectListItem> Roles { get; set; } = new()
    {
        new SelectListItem("Người dùng", "User")
    };
}
