namespace Nagaira.Ecommerce.Domain.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public string? PaymentTerms { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public bool IsActive { get; set; }
    
    public List<ProductSupplier> ProductSuppliers { get; set; } = new();
}

