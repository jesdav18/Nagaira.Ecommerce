namespace Nagaira.Ecommerce.Domain.Entities;

public class PaymentMethodType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

