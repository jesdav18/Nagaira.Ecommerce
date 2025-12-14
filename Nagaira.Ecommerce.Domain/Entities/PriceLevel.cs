namespace Nagaira.Ecommerce.Domain.Entities;

public class PriceLevel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public decimal MarkupPercentage { get; set; }
    public bool IsActive { get; set; }
    public List<ProductPrice> ProductPrices { get; set; } = new();
    public List<User> Users { get; set; } = new();
}

