using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<InventoryMovementDto>> GetMovementsByProductAsync(Guid productId)
    {
        var movements = await _unitOfWork.InventoryMovements.GetByProductIdAsync(productId);
        return movements.Select(MapToDto);
    }

    public async Task<IEnumerable<InventoryMovementDto>> GetMovementsByReferenceAsync(string referenceType, Guid referenceId)
    {
        var movements = await _unitOfWork.InventoryMovements.GetByReferenceAsync(referenceType, referenceId);
        return movements.Select(MapToDto);
    }

    public async Task<InventoryBalanceDto?> GetBalanceByProductAsync(Guid productId)
    {
        var balance = await _unitOfWork.InventoryBalances.GetByProductIdAsync(productId);
        return balance != null ? MapBalanceToDto(balance) : null;
    }

    public async Task<IEnumerable<InventoryBalanceDto>> GetLowStockProductsAsync(int threshold)
    {
        var balances = await _unitOfWork.InventoryBalances.GetLowStockProductsAsync(threshold);
        return balances.Select(MapBalanceToDto);
    }

    public async Task<IEnumerable<InventoryBalanceDto>> GetAllProductBalancesAsync()
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        var balances = new List<InventoryBalanceDto>();

        foreach (var product in products)
        {
            var balance = product.InventoryBalance;
            if (balance != null)
            {
                balances.Add(MapBalanceToDto(balance));
            }
            else
            {
                var availableQuantity = await _unitOfWork.InventoryMovements.GetAvailableQuantityAsync(product.Id);
                balances.Add(new InventoryBalanceDto(
                    product.Id,
                    product.Name,
                    availableQuantity,
                    0,
                    DateTime.UtcNow
                ));
            }
        }

        return balances.OrderBy(b => b.ProductName);
    }

    public async Task<InventoryMovementDto> CreateMovementAsync(CreateInventoryMovementDto dto, Guid userId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
        if (product == null) throw new Exception("Product not found");

        InventoryMovementType movementType;
        
        var normalizedType = dto.MovementType.Trim();
        
        movementType = normalizedType switch
        {
            "Purchase" or "purchase" => InventoryMovementType.Purchase,
            "Sale" or "sale" => InventoryMovementType.Sale,
            "Return" or "return" => InventoryMovementType.Return,
            "Adjustment" or "adjustment" => InventoryMovementType.Adjustment,
            "TransferIn" or "transfer_in" or "transferin" => InventoryMovementType.TransferIn,
            "TransferOut" or "transfer_out" or "transferout" => InventoryMovementType.TransferOut,
            "Damage" or "damage" => InventoryMovementType.Damage,
            "Expired" or "expired" => InventoryMovementType.Expired,
            "InitialStock" or "initial_stock" or "initialstock" => InventoryMovementType.InitialStock,
            _ => throw new Exception($"Tipo de movimiento inválido: {dto.MovementType}")
        };

        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            MovementType = movementType,
            Quantity = dto.Quantity,
            ReferenceNumber = dto.ReferenceNumber,
            ReferenceType = dto.ReferenceType,
            ReferenceId = dto.ReferenceId,
            Notes = dto.Notes,
            CostPerUnit = dto.CostPerUnit,
            TotalCost = dto.CostPerUnit.HasValue ? dto.CostPerUnit.Value * dto.Quantity : null,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.InventoryMovements.AddAsync(movement);
        await _unitOfWork.SaveChangesAsync();

        var balance = await _unitOfWork.InventoryBalances.GetByProductIdAsync(dto.ProductId);
        if (balance == null)
        {
            balance = new InventoryBalance
            {
                ProductId = dto.ProductId,
                AvailableQuantity = await _unitOfWork.InventoryMovements.GetAvailableQuantityAsync(dto.ProductId),
                ReservedQuantity = 0,
                LastMovementId = movement.Id,
                LastUpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.InventoryBalances.AddAsync(balance);
        }
        else
        {
            balance.AvailableQuantity = await _unitOfWork.InventoryMovements.GetAvailableQuantityAsync(dto.ProductId);
            balance.LastMovementId = movement.Id;
            balance.LastUpdatedAt = DateTime.UtcNow;
            await _unitOfWork.InventoryBalances.UpdateAsync(balance);
        }

        await _unitOfWork.SaveChangesAsync();
        return MapToDto(movement);
    }

    public async Task<InventoryBalanceDto> GetProductBalanceAsync(Guid productId)
    {
        var balance = await _unitOfWork.InventoryBalances.GetByProductIdAsync(productId);
        if (balance == null)
        {
            var availableQuantity = await _unitOfWork.InventoryMovements.GetAvailableQuantityAsync(productId);
            balance = new InventoryBalance
            {
                ProductId = productId,
                AvailableQuantity = availableQuantity,
                ReservedQuantity = 0,
                LastUpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.InventoryBalances.AddAsync(balance);
            await _unitOfWork.SaveChangesAsync();
        }
        return MapBalanceToDto(balance);
    }

    private static InventoryMovementDto MapToDto(InventoryMovement movement)
    {
        return new InventoryMovementDto(
            movement.Id,
            movement.ProductId,
            movement.Product?.Name ?? string.Empty,
            movement.MovementType.ToString(),
            movement.Quantity,
            movement.ReferenceNumber,
            movement.ReferenceType,
            movement.ReferenceId,
            movement.Notes,
            movement.CostPerUnit,
            movement.TotalCost,
            movement.CreatedBy,
            movement.Creator != null ? $"{movement.Creator.FirstName} {movement.Creator.LastName}" : null,
            movement.CreatedAt
        );
    }

    public async Task<IEnumerable<MovementTypeDto>> GetMovementTypesAsync()
    {
        return new List<MovementTypeDto>
        {
            new MovementTypeDto("Purchase", "Compra", "Entrada"),
            new MovementTypeDto("Sale", "Venta", "Salida"),
            new MovementTypeDto("Return", "Devolución", "Entrada"),
            new MovementTypeDto("Adjustment", "Ajuste", "Ajuste"),
            new MovementTypeDto("TransferIn", "Transferencia Entrada", "Entrada"),
            new MovementTypeDto("TransferOut", "Transferencia Salida", "Salida"),
            new MovementTypeDto("Damage", "Daño", "Salida"),
            new MovementTypeDto("Expired", "Vencido", "Salida"),
            new MovementTypeDto("InitialStock", "Stock Inicial", "Entrada")
        };
    }

    private static InventoryBalanceDto MapBalanceToDto(InventoryBalance balance)
    {
        return new InventoryBalanceDto(
            balance.ProductId,
            balance.Product?.Name ?? string.Empty,
            balance.AvailableQuantity,
            balance.ReservedQuantity,
            balance.LastUpdatedAt
        );
    }
}

