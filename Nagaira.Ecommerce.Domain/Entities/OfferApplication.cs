namespace Nagaira.Ecommerce.Domain.Entities;

public class OfferApplication : BaseEntity
{
    public Guid OfferId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? OrderItemId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? UserId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime AppliedAt { get; set; }
    public Offer Offer { get; set; } = null!;
    public Order? Order { get; set; }
    public OrderItem? OrderItem { get; set; }
    public Product? Product { get; set; }
    public User? User { get; set; }
}

