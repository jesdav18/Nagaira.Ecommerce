using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminOrdersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll([FromQuery] string? status = null, [FromQuery] int take = 100)
    {
        OrderStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var enumStatus))
            {
                return BadRequest(new { message = "Estado no valido." });
            }
            parsedStatus = enumStatus;
        }

        var orders = await _unitOfWork.Orders.GetForAdminAsync(parsedStatus, Math.Clamp(take, 1, 300));
        return Ok(orders.Select(MapToDto));
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
}
