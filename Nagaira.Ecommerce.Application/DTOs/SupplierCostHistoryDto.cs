namespace Nagaira.Ecommerce.Application.DTOs;

public record SupplierCostHistoryDto(
    Guid Id,
    Guid ProductSupplierId,
    string ProductName,
    string SupplierName,
    decimal? OldCost,
    decimal NewCost,
    string? ChangedByUserName,
    string? ChangeReason,
    DateTime CreatedAt
);

