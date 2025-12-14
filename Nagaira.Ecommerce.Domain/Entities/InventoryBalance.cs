namespace Nagaira.Ecommerce.Domain.Entities;

public class InventoryBalance
{
    public Guid ProductId { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public Guid? LastMovementId { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public Product Product { get; set; } = null!;
    public InventoryMovement? LastMovement { get; set; }
}

