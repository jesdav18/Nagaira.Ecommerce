using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class PaymentMethodRepository : Repository<PaymentMethod>, IPaymentMethodRepository
{
    public PaymentMethodRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<PaymentMethod>> GetAllAsync()
    {
        return await _dbSet
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentMethod>> GetActivePaymentMethodsAsync()
    {
        return await _dbSet
            .Where(p => !p.IsDeleted && p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentMethod>> GetByTypeAsync(string type)
    {
        return await _dbSet
            .Where(p => !p.IsDeleted && p.IsActive && p.Type == type)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }
}

