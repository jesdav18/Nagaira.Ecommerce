namespace Nagaira.Ecommerce.Domain.Entities;

public class ProductRequest : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageName { get; set; }
    public string Status { get; set; } = "new";
}
