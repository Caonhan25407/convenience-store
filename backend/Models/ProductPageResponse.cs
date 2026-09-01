namespace StoreWeb.Api.Models;

public class ProductPageResponse
{
    public IReadOnlyList<Product> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
