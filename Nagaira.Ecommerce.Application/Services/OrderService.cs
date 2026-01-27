using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace Nagaira.Ecommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public OrderService(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<OrderDto> CreateOrderAsync(Guid userId, CreateOrderDto dto)
    {
        var strategy = _unitOfWork.GetExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var dbContext = _unitOfWork.GetDbContext();
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) throw new Exception("User not found");

                var orderNumber = GenerateOrderNumber();
                decimal subtotal = 0;

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    UserId = userId,
                    ShippingAddressId = dto.ShippingAddressId,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                decimal totalDiscount = 0;

                var itemInfos = new List<(Product Product, int Quantity, decimal BaseUnitPrice)>();
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

                    var basePrice = await _unitOfWork.ProductPrices
                        .GetPriceForProductAndLevelAsync(item.ProductId, user.PriceLevelId);
                    
                    if (!basePrice.HasValue)
                        throw new Exception($"No price found for product {product.Name}");

                    itemInfos.Add((product, item.Quantity, basePrice.Value));
                    cartBaseTotal += basePrice.Value * item.Quantity;
                }

                foreach (var info in itemInfos)
                {
                    var product = info.Product;
                    var quantity = info.Quantity;
                    var unitPrice = info.BaseUnitPrice;

                    var applicableOffers = await _unitOfWork.Offers.GetOffersForProductAsync(product.Id, DateTime.UtcNow);
                    
                    foreach (var offer in applicableOffers.OrderByDescending(o => o.Priority))
                    {
                        if (offer.TotalMaxUses.HasValue && offer.CurrentUses >= offer.TotalMaxUses.Value)
                            continue;

                        if (offer.MaxUsesPerCustomer.HasValue)
                        {
                            var userUsage = await _unitOfWork.Offers.GetUsageCountAsync(offer.Id, userId);
                            if (userUsage >= offer.MaxUsesPerCustomer.Value)
                                continue;
                        }

                        if (offer.MinQuantity.HasValue && quantity < offer.MinQuantity.Value)
                            continue;

                        if (!OfferRulesSatisfied(offer, unitPrice, quantity, cartBaseTotal))
                            continue;

                        decimal discount = 0;
                        if (offer.OfferType == OfferType.Percentage && offer.DiscountPercentage.HasValue)
                        {
                            discount = unitPrice * (offer.DiscountPercentage.Value / 100);
                        }
                        else if (offer.OfferType == OfferType.FixedAmount && offer.DiscountAmount.HasValue)
                        {
                            discount = offer.DiscountAmount.Value;
                        }

                        if (discount > 0)
                        {
                            unitPrice -= discount;
                            totalDiscount += discount * quantity;

                            var offerApplication = new OfferApplication
                            {
                                Id = Guid.NewGuid(),
                                OfferId = offer.Id,
                                OrderId = order.Id,
                                OrderItemId = null,
                                ProductId = product.Id,
                                UserId = userId,
                                DiscountAmount = discount * quantity,
                                AppliedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _unitOfWork.Repository<OfferApplication>().AddAsync(offerApplication);

                            offer.CurrentUses++;
                            await _unitOfWork.Offers.UpdateAsync(offer);
                        }
                    }

                    var itemSubtotal = unitPrice * quantity;
                    subtotal += itemSubtotal;

                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Subtotal = itemSubtotal,
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

                order.Subtotal = subtotal;
                order.Tax = (subtotal - totalDiscount) * 0.16m;
                order.ShippingCost = 0;
                order.Total = order.Subtotal + order.Tax + order.ShippingCost;

                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    var savedOrder = await _unitOfWork.Orders.GetByIdAsync(order.Id);
                    if (savedOrder != null)
                    {
                        await _emailService.SendOrderConfirmationAsync(savedOrder, user);
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

    private async Task<OrderDto> GetOrderDtoAsync(Guid orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        return order != null ? MapToDto(order) : throw new Exception("Order not found");
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CreatedAt,
            order.Subtotal,
            order.Tax,
            order.ShippingCost,
            order.Total,
            order.Status.ToString(),
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
            ) : null
        );
    }

    private static bool OfferRulesSatisfied(Offer offer, decimal itemUnitPrice, int quantity, decimal cartTotal)
    {
        if (offer.Rules == null || offer.Rules.Count == 0) return true;

        var itemSubtotal = itemUnitPrice * quantity;
        foreach (var rule in offer.Rules.Where(r => !r.IsDeleted))
        {
            var type = rule.RuleType?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(type)) return false;

            switch (type)
            {
                case "min_item_price":
                    if (itemUnitPrice < rule.Value) return false;
                    break;
                case "max_item_price":
                    if (itemUnitPrice > rule.Value) return false;
                    break;
                case "min_item_subtotal":
                    if (itemSubtotal < rule.Value) return false;
                    break;
                case "max_item_subtotal":
                    if (itemSubtotal > rule.Value) return false;
                    break;
                case "min_cart_total":
                    if (cartTotal < rule.Value) return false;
                    break;
                default:
                    return false;
            }
        }

        return true;
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
