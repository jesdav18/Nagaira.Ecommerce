namespace Nagaira.Ecommerce.Domain.Entities;

public class OfferExcludedCategory : BaseEntity
{
    public Guid OfferId { get; set; }
    public Guid CategoryId { get; set; }
    public Offer Offer { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
