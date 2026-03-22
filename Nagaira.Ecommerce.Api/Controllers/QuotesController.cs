using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Pricing;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/quotes")]
[AllowAnonymous]
public class QuotesController : ControllerBase
{
    private static readonly HashSet<string> AllowedCustomerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "named",
        "consumer_final"
    };

    private readonly IUnitOfWork _unitOfWork;

    public QuotesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<ActionResult<QuoteDto>> Create([FromBody] CreateQuoteDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(new { message = "La cotizacion debe incluir al menos un producto." });
        }

        var customerType = (dto.CustomerType ?? string.Empty).Trim().ToLowerInvariant();
        if (!AllowedCustomerTypes.Contains(customerType))
        {
            return BadRequest(new { message = "Tipo de cliente no valido." });
        }

        if (customerType == "named")
        {
            if (string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                return BadRequest(new { message = "El nombre del cliente es obligatorio." });
            }

            if (string.IsNullOrWhiteSpace(dto.CustomerTaxId))
            {
                return BadRequest(new { message = "El RTN es obligatorio para cotizacion con nombre." });
            }
        }

        var dbContext = _unitOfWork.GetDbContext();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        Quote? createdQuote = null;

        try
        {
            await strategy.ExecuteAsync(
                state: 0,
                operation: async (_, _, ct) =>
                {
                    await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

                    var sanitizedName = customerType == "consumer_final"
                        ? "Consumidor final"
                        : dto.CustomerName.Trim();
                    var sanitizedTaxId = customerType == "consumer_final"
                        ? null
                        : dto.CustomerTaxId?.Trim();

                    var items = new List<QuoteItem>();
                    decimal orderTotal = 0m;

                    foreach (var item in dto.Items)
                    {
                        if (item.Quantity <= 0)
                        {
                            throw new InvalidOperationException("La cantidad debe ser mayor a cero.");
                        }

                        var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                        if (product == null || product.IsDeleted || !product.IsActive)
                        {
                            throw new InvalidOperationException($"Producto no valido: {item.ProductId}");
                        }

                        var prices = await _unitOfWork.ProductPrices.GetByProductIdAsync(item.ProductId);
                        var unitPrice = ProductPriceResolver.ResolveUnitPrice(prices, item.Quantity);
                        if (!unitPrice.HasValue)
                        {
                            throw new InvalidOperationException($"No price found for product {product.Name}");
                        }

                        var originalUnitPrice = prices
                            .Where(p => p.IsActive && !p.IsDeleted)
                            .OrderBy(p => p.MinQuantity)
                            .Select(p => (decimal?)p.Price)
                            .FirstOrDefault();

                        var lineSubtotal = Math.Round(unitPrice.Value * item.Quantity, 2);
                        orderTotal += lineSubtotal;

                        items.Add(new QuoteItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            ProductName = product.Name,
                            Sku = product.Sku,
                            Quantity = item.Quantity,
                            UnitPrice = Math.Round(unitPrice.Value, 2),
                            UnitPriceOriginal = originalUnitPrice.HasValue && originalUnitPrice.Value > unitPrice.Value
                                ? Math.Round(originalUnitPrice.Value, 2)
                                : null,
                            Subtotal = lineSubtotal,
                            CreatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        });
                    }

                    var taxRate = dto.TaxRate < 0 ? 0 : dto.TaxRate;
                    var shippingAmount = dto.ShippingAmount < 0 ? 0 : Math.Round(dto.ShippingAmount, 2);
                    var discount = dto.Discount < 0 ? 0 : Math.Round(dto.Discount, 2);
                    var taxableBase = Math.Max(orderTotal - discount, 0);
                    var subtotal = Math.Round(taxableBase / (1 + taxRate), 2);
                    var tax = Math.Round(taxableBase - subtotal, 2);
                    var total = Math.Round(subtotal + tax + shippingAmount, 2);

                    var quote = new Quote
                    {
                        Id = Guid.NewGuid(),
                        QuoteNumber = GenerateQuoteNumber(),
                        CustomerName = sanitizedName,
                        CustomerTaxId = sanitizedTaxId,
                        CustomerType = customerType,
                        CurrencySymbol = string.IsNullOrWhiteSpace(dto.CurrencySymbol) ? "$" : dto.CurrencySymbol.Trim(),
                        TaxLabel = string.IsNullOrWhiteSpace(dto.TaxLabel) ? "Impuestos" : dto.TaxLabel.Trim(),
                        TaxRate = taxRate,
                        Subtotal = subtotal,
                        Tax = tax,
                        ShippingAmount = shippingAmount,
                        Discount = discount,
                        Total = total,
                        Status = "draft",
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                        Items = items
                    };

                    foreach (var item in items)
                    {
                        item.QuoteId = quote.Id;
                    }

                    await _unitOfWork.Repository<Quote>().AddAsync(quote);
                    await _unitOfWork.SaveChangesAsync();
                    await tx.CommitAsync(ct);
                    createdQuote = quote;
                    return 0;
                },
                verifySucceeded: null,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (createdQuote == null)
        {
            return StatusCode(500, new { message = "No se pudo generar la cotizacion." });
        }

        return Ok(MapToDto(createdQuote));
    }

    private static QuoteDto MapToDto(Quote quote)
    {
        return new QuoteDto(
            quote.Id,
            quote.QuoteNumber,
            quote.CreatedAt,
            quote.CustomerName,
            quote.CustomerTaxId,
            quote.CustomerType,
            quote.Subtotal,
            quote.Tax,
            quote.ShippingAmount,
            quote.Discount,
            quote.Total,
            quote.CurrencySymbol,
            quote.TaxLabel,
            quote.Items.Select(i => new QuoteItemDto(
                i.ProductId,
                i.ProductName,
                i.Sku,
                i.Quantity,
                i.UnitPrice,
                i.UnitPriceOriginal,
                i.Subtotal
            )).ToList()
        );
    }

    private static string GenerateQuoteNumber()
    {
        return $"COT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}
