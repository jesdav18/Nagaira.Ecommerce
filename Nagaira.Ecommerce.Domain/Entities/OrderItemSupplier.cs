namespace Nagaira.Ecommerce.Domain.Entities;

public class OrderItemSupplier : BaseEntity
{
    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public Guid ProductSupplierId { get; set; }
    public ProductSupplier ProductSupplier { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

