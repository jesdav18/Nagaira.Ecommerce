using System.ComponentModel.DataAnnotations;

namespace Nagaira.Ecommerce.Application.DTOs;

public record PaymentMethodDto(
    Guid Id,
    string Name,
    string Description,
    string Type,
    string TypeLabel,
    string? AccountNumber,
    string? BankName,
    string? AccountHolderName,
    string? WalletProvider,
    string? WalletNumber,
    string? QrCodeUrl,
    string? Instructions,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAt
);

public record CreatePaymentMethodDto(
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
    string Name,
    
    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    string? Description,
    
    [Required(ErrorMessage = "El tipo de medio de pago es requerido")]
    string Type,
    
    [StringLength(100, ErrorMessage = "El número de cuenta no puede exceder 100 caracteres")]
    string? AccountNumber,
    
    [StringLength(255, ErrorMessage = "El nombre del banco no puede exceder 255 caracteres")]
    string? BankName,
    
    [StringLength(255, ErrorMessage = "El nombre del titular no puede exceder 255 caracteres")]
    string? AccountHolderName,
    
    [StringLength(255, ErrorMessage = "El proveedor de billetera no puede exceder 255 caracteres")]
    string? WalletProvider,
    
    [StringLength(100, ErrorMessage = "El número de billetera no puede exceder 100 caracteres")]
    string? WalletNumber,
    
    [StringLength(500, ErrorMessage = "La URL del QR no puede exceder 500 caracteres")]
    string? QrCodeUrl,
    
    [StringLength(2000, ErrorMessage = "Las instrucciones no pueden exceder 2000 caracteres")]
    string? Instructions,
    
    int DisplayOrder,
    
    bool IsActive
);

public record UpdatePaymentMethodDto(
    Guid Id,
    
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(255, ErrorMessage = "El nombre no puede exceder 255 caracteres")]
    string Name,
    
    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    string? Description,
    
    [Required(ErrorMessage = "El tipo de medio de pago es requerido")]
    string Type,
    
    [StringLength(100, ErrorMessage = "El número de cuenta no puede exceder 100 caracteres")]
    string? AccountNumber,
    
    [StringLength(255, ErrorMessage = "El nombre del banco no puede exceder 255 caracteres")]
    string? BankName,
    
    [StringLength(255, ErrorMessage = "El nombre del titular no puede exceder 255 caracteres")]
    string? AccountHolderName,
    
    [StringLength(255, ErrorMessage = "El proveedor de billetera no puede exceder 255 caracteres")]
    string? WalletProvider,
    
    [StringLength(100, ErrorMessage = "El número de billetera no puede exceder 100 caracteres")]
    string? WalletNumber,
    
    [StringLength(500, ErrorMessage = "La URL del QR no puede exceder 500 caracteres")]
    string? QrCodeUrl,
    
    [StringLength(2000, ErrorMessage = "Las instrucciones no pueden exceder 2000 caracteres")]
    string? Instructions,
    
    int DisplayOrder,
    
    bool IsActive
);

