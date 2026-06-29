using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class UserAddressResponse
{
    public int AddressId { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class UserAddressCreateRequest
{
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public string FullAddress { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
