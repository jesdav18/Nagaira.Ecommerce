namespace Nagaira.Ecommerce.Application.DTOs;

public record QuoteDto(
    Guid Id,
    string QuoteNumber,
    DateTime CreatedAt,
    string CustomerName,
    string? CustomerTaxId,
    string CustomerType,
    decimal Subtotal,
    decimal Tax,
    decimal ShippingAmount,
    decimal Discount,
    decimal Total,
    string CurrencySymbol,
    string TaxLabel,
    List<QuoteItemDto> Items
);

public record QuoteItemDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal? UnitPriceOriginal,
    decimal Subtotal
);

public record CreateQuoteDto(
    string CustomerName,
    string? CustomerTaxId,
    string CustomerType,
    string CurrencySymbol,
    string TaxLabel,
    decimal TaxRate,
    decimal Subtotal,
    decimal Tax,
    decimal ShippingAmount,
    decimal Discount,
    decimal Total,
    List<CreateQuoteItemDto> Items
);

public record CreateQuoteItemDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal? UnitPriceOriginal,
    decimal Subtotal
);
