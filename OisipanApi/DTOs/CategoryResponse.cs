namespace Oishipan.DTOs;

public class CategoryResponse
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int ProductCount { get; set; }
}
