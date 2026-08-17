namespace Nagaira.Ecommerce.Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<Product> Products { get; set; } = [];
}
