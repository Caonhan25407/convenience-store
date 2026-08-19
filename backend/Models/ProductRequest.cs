namespace StoreWeb.Api.Models;

public class ProductRequest
{
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }
}