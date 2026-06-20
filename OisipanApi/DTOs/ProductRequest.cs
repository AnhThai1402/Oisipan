using System.ComponentModel.DataAnnotations;

namespace Oishipan.DTOs;

public class ProductRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
    public decimal Price { get; set; }

    public string? Image { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm.")]
    public int Quantity { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thương hiệu.")]
    public string? Description { get; set; }

    public List<ProductOptionInputRequest> ProductOptions { get; set; } = new();

    public List<ProductVariantRequest> ProductVariants { get; set; } = new();
}

public class ProductVariantRequest
{
    public int ProductVariantId { get; set; }

    [Required(ErrorMessage = "Vui long nhap size.")]
    [StringLength(100, ErrorMessage = "Size khong duoc vuot qua 100 ky tu.")]
    public string Size { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap nhan banh.")]
    [StringLength(100, ErrorMessage = "Nhan banh khong duoc vuot qua 100 ky tu.")]
    public string Filling { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Gia tang them khong duoc am.")]
    public decimal AdditionalPrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "So luong khong duoc am.")]
    public int Quantity { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ProductVariantManageRequest : ProductVariantRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui long chon san pham.")]
    public int ProductId { get; set; }
}

public class ProductOptionInputRequest
{
    [Required(ErrorMessage = "Vui long nhap ten tuy chon.")]
    [StringLength(100, ErrorMessage = "Ten tuy chon khong duoc vuot qua 100 ky tu.")]
    public string OptionName { get; set; } = string.Empty;

    public List<ProductValueInputRequest> ProductValues { get; set; } = new();
}

public class ProductValueInputRequest
{
    [Required(ErrorMessage = "Vui long nhap gia tri tuy chon.")]
    [StringLength(100, ErrorMessage = "Gia tri tuy chon khong duoc vuot qua 100 ky tu.")]
    public string ValueName { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Gia tang them khong duoc am.")]
    public decimal AdditionalPrice { get; set; }
}
