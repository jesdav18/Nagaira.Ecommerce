using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId);
}
