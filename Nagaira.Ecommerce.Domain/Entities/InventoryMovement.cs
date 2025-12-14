namespace Nagaira.Ecommerce.Domain.Entities;

public class InventoryMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public InventoryMovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public decimal? CostPerUnit { get; set; }
    public decimal? TotalCost { get; set; }
    public Guid? CreatedBy { get; set; }
    public Product Product { get; set; } = null!;
    public User? Creator { get; set; }
}

public enum InventoryMovementType
{
    Purchase = 1,
    Sale = 2,
    Return = 3,
    Adjustment = 4,
    TransferIn = 5,
    TransferOut = 6,
    Damage = 7,
    Expired = 8,
    InitialStock = 9
}

