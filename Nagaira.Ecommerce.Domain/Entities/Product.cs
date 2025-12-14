namespace Nagaira.Ecommerce.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public List<ProductImage> Images { get; set; } = new();
    public List<ProductPrice> Prices { get; set; } = new();
    public List<InventoryMovement> InventoryMovements { get; set; } = new();
    public InventoryBalance? InventoryBalance { get; set; }
    public List<OfferProduct> OfferProducts { get; set; } = new();
    public bool IsActive { get; set; }
    public decimal? Cost { get; set; }
}
