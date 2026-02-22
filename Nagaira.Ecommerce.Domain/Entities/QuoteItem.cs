namespace Nagaira.Ecommerce.Domain.Entities;

public class QuoteItem : BaseEntity
{
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? UnitPriceOriginal { get; set; }
    public decimal Subtotal { get; set; }
}

