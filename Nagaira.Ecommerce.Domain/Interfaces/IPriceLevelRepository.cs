using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IPriceLevelRepository : IRepository<PriceLevel>
{
    Task<PriceLevel?> GetByNameAsync(string name);
    Task<IEnumerable<PriceLevel>> GetActiveLevelsAsync();
}

