using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Entities;

public class OfferRule : BaseEntity
{
    public Guid OfferId { get; set; }
    public Offer Offer { get; set; } = null!;
    public string RuleType { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
