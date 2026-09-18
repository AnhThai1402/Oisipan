namespace Oishipan.DTOs;

public class WishlistItemResponse
{
    public Guid WishlistItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public short StockQuantity { get; set; }
    public DateTime AddedAt { get; set; }
}