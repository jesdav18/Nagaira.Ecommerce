namespace Nagaira.Ecommerce.Domain.Entities;

public class ProductSupplier : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierSku { get; set; }
    public decimal SupplierCost { get; set; }
    public bool IsPrimary { get; set; }
    public int Priority { get; set; } = 1;
    public int LeadTimeDays { get; set; }
    public int MinOrderQuantity { get; set; } = 1;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    
    public Product Product { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
}

