using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IOfferRepository : IRepository<Offer>
{
    Task<IEnumerable<Offer>> GetActiveOffersAsync(DateTime date);
    Task<IEnumerable<Offer>> GetOffersForProductAsync(Guid productId, DateTime date);
    Task<IEnumerable<Offer>> GetOffersForCategoryAsync(Guid categoryId, DateTime date);
    Task<IEnumerable<Offer>> GetOffersForUserAsync(Guid userId, DateTime date);
    Task<int> GetUsageCountAsync(Guid offerId, Guid? userId);
}

