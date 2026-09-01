namespace StoreWeb.Api.Models;

public sealed class OrderPageResponse
{
    public IReadOnlyList<OrderListItemResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public sealed class OrderListItemResponse
{
    public long Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public int TotalQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<OrderLineItemResponse> Items { get; set; } = [];
}

public sealed class OrderLineItemResponse
{
    public int? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
