namespace Nagaira.Ecommerce.Domain.Entities;

public class Quote : BaseEntity
{
    public string QuoteNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxId { get; set; }
    public string CustomerType { get; set; } = "consumer_final";
    public string CurrencySymbol { get; set; } = "$";
    public string TaxLabel { get; set; } = "Impuestos";
    public decimal TaxRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "draft";
    public List<QuoteItem> Items { get; set; } = new();
}

