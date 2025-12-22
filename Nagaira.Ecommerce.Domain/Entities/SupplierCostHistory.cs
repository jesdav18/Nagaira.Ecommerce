namespace Nagaira.Ecommerce.Domain.Entities;

public class SupplierCostHistory : BaseEntity
{
    public Guid ProductSupplierId { get; set; }
    public ProductSupplier ProductSupplier { get; set; } = null!;
    public decimal? OldCost { get; set; }
    public decimal NewCost { get; set; }
    public Guid? ChangedBy { get; set; }
    public User? ChangedByUser { get; set; }
    public string? ChangeReason { get; set; }
}

