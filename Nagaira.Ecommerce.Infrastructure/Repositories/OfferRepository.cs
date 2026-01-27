using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class OfferRepository : Repository<Offer>, IOfferRepository
{
    public OfferRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Offer?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(o => o.Products)
            .Include(o => o.Categories)
            .Include(o => o.ExcludedProducts)
            .Include(o => o.ExcludedCategories)
            .Include(o => o.Rules)
            .Include(o => o.Creator)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
    }

    public override async Task<IEnumerable<Offer>> GetAllAsync()
    {
        var query = _dbSet
            .Include(o => o.Products)
            .Include(o => o.Categories)
            .Include(o => o.ExcludedProducts)
            .Include(o => o.ExcludedCategories)
            .Include(o => o.Rules)
            .Include(o => o.Creator)
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt);


         return await query
            .ToListAsync();
    }

    public async Task<IEnumerable<Offer>> GetActiveOffersAsync(DateTime date)
    {
        return await _dbSet
            .Include(o => o.Products)
            .Include(o => o.Categories)
            .Include(o => o.ExcludedProducts)
            .Include(o => o.ExcludedCategories)
            .Include(o => o.Rules)
            .Where(o => o.IsActive 
                && o.Status == OfferStatus.Active
                && o.StartDate <= date 
                && o.EndDate >= date
                && !o.IsDeleted)
            .OrderByDescending(o => o.Priority)
            .ToListAsync();
    }

    public async Task<IEnumerable<Offer>> GetOffersForProductAsync(Guid productId, DateTime date)
    {
        return await _dbSet
            .Include(o => o.Products)
            .Include(o => o.Categories)
            .Include(o => o.ExcludedProducts)
            .Include(o => o.ExcludedCategories)
            .Include(o => o.Rules)
            .Where(o => o.IsActive 
                && o.Status == OfferStatus.Active
                && o.StartDate <= date 
                && o.EndDate >= date
                && !o.IsDeleted
                && (
                    (!o.Products.Any(p => !p.IsDeleted) && !o.Categories.Any(c => !c.IsDeleted))
                    || o.Products.Any(p => p.ProductId == productId && !p.IsDeleted)
                    || o.Categories.Any(c => !c.IsDeleted && c.Category.Products.Any(p => p.Id == productId))
                )
                && !o.ExcludedProducts.Any(p => p.ProductId == productId && !p.IsDeleted)
                && !o.ExcludedCategories.Any(c => !c.IsDeleted && c.Category.Products.Any(p => p.Id == productId)))
            .OrderByDescending(o => o.Priority)
            .ToListAsync();
    }

    public async Task<IEnumerable<Offer>> GetOffersForCategoryAsync(Guid categoryId, DateTime date)
    {
        return await _dbSet
            .Include(o => o.Categories)
            .Include(o => o.ExcludedCategories)
            .Include(o => o.Rules)
            .Where(o => o.IsActive 
                && o.Status == OfferStatus.Active
                && o.StartDate <= date 
                && o.EndDate >= date
                && !o.IsDeleted
                && (
                    (!o.Products.Any(p => !p.IsDeleted) && !o.Categories.Any(c => !c.IsDeleted))
                    || o.Categories.Any(c => c.CategoryId == categoryId && !c.IsDeleted)
                )
                && !o.ExcludedCategories.Any(c => c.CategoryId == categoryId && !c.IsDeleted))
            .OrderByDescending(o => o.Priority)
            .ToListAsync();
    }

    public async Task<IEnumerable<Offer>> GetOffersForUserAsync(Guid userId, DateTime date)
    {
        return await _dbSet
            .Include(o => o.Products)
            .Include(o => o.Categories)
            .Include(o => o.ExcludedProducts)
            .Include(o => o.ExcludedCategories)
            .Include(o => o.Rules)
            .Where(o => o.IsActive 
                && o.Status == OfferStatus.Active
                && o.StartDate <= date 
                && o.EndDate >= date
                && !o.IsDeleted)
            .OrderByDescending(o => o.Priority)
            .ToListAsync();
    }

    public async Task<int> GetUsageCountAsync(Guid offerId, Guid? userId)
    {
        return await _context.Set<OfferApplication>()
            .CountAsync(a => a.OfferId == offerId 
                && (!userId.HasValue || a.UserId == userId));
    }
}

