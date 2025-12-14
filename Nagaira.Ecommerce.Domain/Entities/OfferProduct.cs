namespace Nagaira.Ecommerce.Domain.Entities;

public class OfferProduct : BaseEntity
{
    public Guid OfferId { get; set; }
    public Guid ProductId { get; set; }
    public Offer Offer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

