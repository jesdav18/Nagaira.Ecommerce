using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
}
