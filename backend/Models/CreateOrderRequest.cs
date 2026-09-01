namespace StoreWeb.Api.Models;

public sealed class CreateOrderRequest
{
    public string? CustomerName { get; set; }
    public string? Phone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? PaymentMethod { get; set; }
    public List<CreateOrderItemRequest?>? Items { get; set; }
}

public sealed class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
