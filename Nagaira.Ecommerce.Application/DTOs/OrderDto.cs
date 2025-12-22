namespace Nagaira.Ecommerce.Application.DTOs;

public record OrderDto(
    Guid Id,
    string OrderNumber,
    DateTime CreatedAt,
    decimal Subtotal,
    decimal Tax,
    decimal ShippingCost,
    decimal Total,
    string Status,
    List<OrderItemDto> Items,
    AddressDto? ShippingAddress
);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    decimal? AverageCost,
    List<OrderItemSupplierDto>? Suppliers
);

public record OrderItemSupplierDto(
    Guid ProductSupplierId,
    string SupplierName,
    int Quantity,
    decimal UnitCost,
    decimal TotalCost
);

public record CreateOrderDto(
    List<CreateOrderItemDto> Items,
    Guid? ShippingAddressId
);

public record CreateOrderItemDto(
    Guid ProductId,
    int Quantity
);
