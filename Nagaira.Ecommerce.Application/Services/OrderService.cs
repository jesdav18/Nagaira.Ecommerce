using System.Data;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Application.Pricing;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Nagaira.Ecommerce.Application.Services;

public class OrderService : IOrderService
{
    private const decimal FreeShippingThreshold = 700m;
    private const decimal StandardShippingCost = 100m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public OrderService(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<OrderDto> CreateOrderAsync(Guid? userId, CreateOrderDto dto)
    {
        var strategy = _unitOfWork.GetExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                if (dto.Items == null || dto.Items.Count == 0)
                    throw new Exception("La orden debe incluir al menos un producto");

                var customerName = dto.CustomerName?.Trim() ?? string.Empty;
                var customerEmail = dto.CustomerEmail?.Trim() ?? string.Empty;
                var customerPhone = dto.CustomerPhone?.Trim() ?? string.Empty;
                var shippingStreet = dto.ShippingStreet?.Trim() ?? string.Empty;
                var shippingCity = dto.ShippingCity?.Trim() ?? string.Empty;
                var shippingPostalCode = dto.ShippingPostalCode?.Trim() ?? string.Empty;
                var shippingCountry = dto.ShippingCountry?.Trim() ?? string.Empty;
                var paymentMethodName = dto.PaymentMethodName?.Trim() ?? string.Empty;
                var paymentProofImageUrl = dto.PaymentProofImageUrl?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(customerPhone))
                    throw new Exception("El telefono del cliente es obligatorio");

                User? user = null;
                if (userId.HasValue)
                {
                    user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                    if (user == null) throw new Exception("User not found");
                }

                var orderNumber = GenerateOrderNumber();
                decimal orderTotal = 0;

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    UserId = userId,
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    CustomerPhone = customerPhone,
                    ShippingStreet = shippingStreet,
                    ShippingCity = shippingCity,
                    ShippingPostalCode = shippingPostalCode,
                    ShippingCountry = shippingCountry,
                    PaymentMethodId = dto.PaymentMethodId,
                    PaymentMethodName = paymentMethodName,
                    PaymentProofImageUrl = paymentProofImageUrl,
                    ShippingAddressId = dto.ShippingAddressId,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                var taxRate = 0.16m;
                var itemInfos = new List<(Product Product, int Quantity, decimal BaseUnitPrice, List<Offer> ApplicableOffers)>();
                decimal cartBaseTotal = 0;

                foreach (var item in dto.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product == null) throw new Exception($"Product {item.ProductId} not found");

                    if (!product.HasVirtualStock)
                    {
                        var balance = await _unitOfWork.InventoryBalances.GetByProductIdAsync(item.ProductId);
                        var availableQuantity = balance?.AvailableQuantity ?? 0;
                        if (availableQuantity < item.Quantity)
                            throw new Exception($"Insufficient stock for {product.Name}. Available: {availableQuantity}");
                    }

                    var prices = await _unitOfWork.ProductPrices.GetByProductIdAsync(item.ProductId);
                    var applicableOffers = (await _unitOfWork.Offers.GetOffersForProductAsync(product.Id, DateTime.UtcNow)).ToList();
                    var basePrice = ProductPriceResolver.ResolveUnitPrice(prices, item.Quantity);
                    if (!basePrice.HasValue)
                        throw new Exception($"No price found for product {product.Name}");

                    itemInfos.Add((product, item.Quantity, basePrice.Value, applicableOffers));
                    cartBaseTotal += basePrice.Value * item.Quantity;
                }

                var appliedOffers = new Dictionary<Guid, (Offer Offer, decimal TotalDiscount)>();
                foreach (var info in itemInfos)
                {
                    var product = info.Product;
                    var quantity = info.Quantity;
                    var unitPrice = info.BaseUnitPrice;

                    var eligibleOffers = new List<Offer>();
                    foreach (var offer in info.ApplicableOffers)
                    {
                        var alreadyAppliedInOrder = appliedOffers.ContainsKey(offer.Id);
                        if (!alreadyAppliedInOrder
                            && offer.TotalMaxUses.HasValue
                            && offer.CurrentUses >= offer.TotalMaxUses.Value)
                            continue;

                        if (offer.MaxUsesPerCustomer.HasValue)
                        {
                            if (!userId.HasValue)
                                continue;

                            var userUsage = await _unitOfWork.Offers.GetUsageCountAsync(offer.Id, userId.Value);
                            if (userUsage >= offer.MaxUsesPerCustomer.Value)
                                continue;
                        }
                        eligibleOffers.Add(offer);
                    }

                    var offerResult = OfferPriceCalculator.SelectOffer(
                        eligibleOffers,
                        unitPrice,
                        quantity,
                        cartBaseTotal);
                    if (offerResult != null)
                    {
                        unitPrice = offerResult.FinalUnitPrice;
                        if (appliedOffers.TryGetValue(offerResult.Offer.Id, out var existingApplication))
                        {
                            appliedOffers[offerResult.Offer.Id] = (
                                existingApplication.Offer,
                                existingApplication.TotalDiscount + offerResult.TotalDiscount);
                        }
                        else
                        {
                            appliedOffers[offerResult.Offer.Id] = (offerResult.Offer, offerResult.TotalDiscount);
                            offerResult.Offer.CurrentUses++;
                            await _unitOfWork.Offers.UpdateAsync(offerResult.Offer);
                        }
                    }

                    var itemTotal = unitPrice * quantity;
                    orderTotal += itemTotal;

                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Subtotal = itemTotal,
                        CreatedAt = DateTime.UtcNow
                    };

                    var supplierDistribution = await DistributeQuantityAmongSuppliersAsync(product.Id, quantity);
                    decimal totalCost = 0;

                    foreach (var dist in supplierDistribution)
                    {
                        var orderItemSupplier = new OrderItemSupplier
                        {
                            Id = Guid.NewGuid(),
                            OrderItemId = orderItem.Id,
                            ProductSupplierId = dist.ProductSupplierId,
                            Quantity = dist.Quantity,
                            UnitCost = dist.UnitCost,
                            TotalCost = dist.TotalCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        orderItem.OrderItemSuppliers.Add(orderItemSupplier);
                        totalCost += dist.TotalCost;
                    }

                    if (supplierDistribution.Any())
                    {
                        var averageCost = totalCost / quantity;
                        product.Cost = averageCost;
                        await _unitOfWork.Products.UpdateAsync(product);
                    }

                    order.Items.Add(orderItem);

                    var inventoryMovement = new InventoryMovement
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        MovementType = InventoryMovementType.Sale,
                        Quantity = quantity,
                        ReferenceType = "Order",
                        ReferenceId = order.Id,
                        ReferenceNumber = orderNumber,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.InventoryMovements.AddAsync(inventoryMovement);
                }

                foreach (var appliedOffer in appliedOffers.Values)
                {
                    await _unitOfWork.Repository<OfferApplication>().AddAsync(new OfferApplication
                    {
                        Id = Guid.NewGuid(),
                        OfferId = appliedOffer.Offer.Id,
                        OrderId = order.Id,
                        OrderItemId = null,
                        ProductId = null,
                        UserId = userId,
                        DiscountAmount = appliedOffer.TotalDiscount,
                        AppliedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                var shippingCost = orderTotal >= FreeShippingThreshold ? 0m : StandardShippingCost;

                order.Total = orderTotal + shippingCost;
                order.Subtotal = orderTotal / (1 + taxRate);
                order.Tax = orderTotal - order.Subtotal;
                order.ShippingCost = shippingCost;

                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    var savedOrder = await _unitOfWork.Orders.GetByIdAsync(order.Id);
                    if (savedOrder != null)
                    {
                        var recipientName = user == null
                            ? customerName
                            : $"{user.FirstName} {user.LastName}".Trim();
                        var recipientEmail = user?.Email ?? customerEmail;
                        await _emailService.SendOrderConfirmationAsync(savedOrder, recipientEmail, recipientName);
                    }
                }
                catch
                {
                }

                return await GetOrderDtoAsync(order.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await _unitOfWork.Orders.GetByUserIdAsync(userId);
        return orders.Select(MapToDto);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        return order != null ? MapToDto(order) : null;
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, string status)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null) throw new Exception("Order not found");

        order.Status = Enum.Parse<OrderStatus>(status);
        order.UpdatedAt = DateTime.UtcNow;

        if (order.Status == OrderStatus.Delivered)
            order.CompletedAt = DateTime.UtcNow;

        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<OrderDto> UpdatePaymentProofAsync(Guid orderId, UpdatePaymentProofDto dto)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null) throw new Exception("Order not found");

        var imageUrl = dto.PaymentProofImageUrl?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new Exception("La URL del comprobante es obligatoria");

        order.PaymentProofImageUrl = imageUrl;
        order.PaymentMethodId = dto.PaymentMethodId;
        order.PaymentMethodName = dto.PaymentMethodName?.Trim() ?? order.PaymentMethodName;

        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return await GetOrderDtoAsync(orderId);
    }

    private async Task<OrderDto> GetOrderDtoAsync(Guid orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        return order != null ? MapToDto(order) : throw new Exception("Order not found");
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.UserId,
            order.OrderNumber,
            order.CreatedAt,
            order.CustomerName,
            order.CustomerEmail,
            order.CustomerPhone,
            order.Subtotal,
            order.Tax,
            order.ShippingCost,
            order.Total,
            order.Status.ToString(),
            order.PaymentMethodId,
            order.PaymentMethodName,
            order.PaymentProofImageUrl,
            order.Items.Select(i =>
            {
                var suppliers = i.OrderItemSuppliers?.Select(ois => new OrderItemSupplierDto(
                    ois.ProductSupplierId,
                    ois.ProductSupplier?.Supplier?.Name ?? "Proveedor desconocido",
                    ois.Quantity,
                    ois.UnitCost,
                    ois.TotalCost
                )).ToList();

                decimal? averageCost = null;
                if (i.OrderItemSuppliers != null && i.OrderItemSuppliers.Any())
                {
                    var totalCost = i.OrderItemSuppliers.Sum(ois => ois.TotalCost);
                    averageCost = totalCost / i.Quantity;
                }

                return new OrderItemDto(
                    i.ProductId,
                    i.Product?.Name ?? string.Empty,
                    i.Product?.Sku ?? string.Empty,
                    i.Quantity,
                    i.UnitPrice,
                    i.Subtotal,
                    averageCost,
                    suppliers
                );
            }).ToList(),
            order.ShippingAddress != null ? new AddressDto(
                order.ShippingAddress.Id,
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.State,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.Country,
                order.ShippingAddress.IsDefault
            ) : (!string.IsNullOrWhiteSpace(order.ShippingStreet) ? new AddressDto(
                Guid.Empty,
                order.ShippingStreet,
                order.ShippingCity,
                order.ShippingCity,
                order.ShippingPostalCode,
                order.ShippingCountry,
                false
            ) : null)
        );
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private async Task<List<SupplierDistribution>> DistributeQuantityAmongSuppliersAsync(Guid productId, int totalQuantity)
    {
        var suppliers = await _unitOfWork.ProductSuppliers.GetOrderedByPriorityAsync(productId);
        var distribution = new List<SupplierDistribution>();
        int remainingQuantity = totalQuantity;

        foreach (var supplier in suppliers)
        {
            if (remainingQuantity <= 0) break;

            int quantityToAssign = remainingQuantity;

            if (supplier.MinOrderQuantity > 0 && quantityToAssign < supplier.MinOrderQuantity)
            {
                if (distribution.Count == 0)
                {
                    quantityToAssign = supplier.MinOrderQuantity;
                }
                else
                {
                    continue;
                }
            }

            var supplierDist = new SupplierDistribution
            {
                ProductSupplierId = supplier.Id,
                Quantity = quantityToAssign,
                UnitCost = supplier.SupplierCost,
                TotalCost = supplier.SupplierCost * quantityToAssign
            };

            distribution.Add(supplierDist);
            remainingQuantity -= quantityToAssign;
        }

        if (remainingQuantity > 0 && suppliers.Any())
        {
            var lastSupplier = suppliers.LastOrDefault();
            if (lastSupplier != null)
            {
                var lastDist = distribution.LastOrDefault();
                if (lastDist != null && lastDist.ProductSupplierId == lastSupplier.Id)
                {
                    lastDist.Quantity += remainingQuantity;
                    lastDist.TotalCost = lastDist.UnitCost * lastDist.Quantity;
                }
                else
                {
                    distribution.Add(new SupplierDistribution
                    {
                        ProductSupplierId = lastSupplier.Id,
                        Quantity = remainingQuantity,
                        UnitCost = lastSupplier.SupplierCost,
                        TotalCost = lastSupplier.SupplierCost * remainingQuantity
                    });
                }
            }
        }

        return distribution;
    }

    private class SupplierDistribution
    {
        public Guid ProductSupplierId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}
