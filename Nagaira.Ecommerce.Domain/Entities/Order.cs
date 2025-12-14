namespace Nagaira.Ecommerce.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public Guid? ShippingAddressId { get; set; }
    public Address? ShippingAddress { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public DateTime? CompletedAt { get; set; }
}

public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
