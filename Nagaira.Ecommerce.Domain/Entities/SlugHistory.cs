namespace Nagaira.Ecommerce.Domain.Entities;

public class SlugHistory : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Slug { get; set; } = string.Empty;
}
