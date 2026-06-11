namespace Oishipan.DTOs;

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
}
