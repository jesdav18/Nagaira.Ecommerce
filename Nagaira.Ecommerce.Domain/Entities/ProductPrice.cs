namespace Nagaira.Ecommerce.Domain.Entities;

public class ProductPrice : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid PriceLevelId { get; set; }
    public decimal Price { get; set; }
    public int MinQuantity { get; set; } = 1;
    public bool IsActive { get; set; }
    public Product Product { get; set; } = null!;
    public PriceLevel PriceLevel { get; set; } = null!;
}

