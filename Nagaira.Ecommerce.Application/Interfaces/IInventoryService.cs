using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IInventoryService
{
    Task<IEnumerable<InventoryMovementDto>> GetMovementsByProductAsync(Guid productId);
    Task<IEnumerable<InventoryMovementDto>> GetMovementsByReferenceAsync(string referenceType, Guid referenceId);
    Task<InventoryBalanceDto?> GetBalanceByProductAsync(Guid productId);
    Task<IEnumerable<InventoryBalanceDto>> GetLowStockProductsAsync(int threshold);
    Task<IEnumerable<InventoryBalanceDto>> GetAllProductBalancesAsync();
    Task<InventoryMovementDto> CreateMovementAsync(CreateInventoryMovementDto dto, Guid userId);
    Task<InventoryBalanceDto> GetProductBalanceAsync(Guid productId);
    Task<IEnumerable<MovementTypeDto>> GetMovementTypesAsync();
}

