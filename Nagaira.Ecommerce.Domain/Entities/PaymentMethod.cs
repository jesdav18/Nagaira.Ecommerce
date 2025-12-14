namespace Nagaira.Ecommerce.Domain.Entities;

public class PaymentMethod : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Ahora es string que referencia al name de PaymentMethodType
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? AccountHolderName { get; set; }
    public string? WalletProvider { get; set; }
    public string? WalletNumber { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? Instructions { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

