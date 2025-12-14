namespace Nagaira.Ecommerce.Domain.Entities;

public class Offer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OfferType OfferType { get; set; }
    public OfferStatus Status { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? MinPurchaseAmount { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxUsesPerCustomer { get; set; }
    public int? TotalMaxUses { get; set; }
    public int CurrentUses { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public List<OfferProduct> Products { get; set; } = new();
    public List<OfferCategory> Categories { get; set; } = new();
    public List<OfferApplication> Applications { get; set; } = new();
    public User? Creator { get; set; }
}

public enum OfferType
{
    Percentage = 1,
    FixedAmount = 2,
    BuyXGetY = 3,
    FreeShipping = 4
}

public enum OfferStatus
{
    Draft = 1,
    Active = 2,
    Paused = 3,
    Expired = 4,
    Cancelled = 5
}

