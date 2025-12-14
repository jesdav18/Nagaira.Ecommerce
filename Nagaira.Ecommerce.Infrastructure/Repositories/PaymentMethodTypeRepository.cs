using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class PaymentMethodTypeRepository : Repository<PaymentMethodType>, IPaymentMethodTypeRepository
{
    public PaymentMethodTypeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<PaymentMethodType>> GetAllAsync()
    {
        return await _dbSet
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentMethodType>> GetActivePaymentMethodTypesAsync()
    {
        return await _dbSet
            .Where(p => !p.IsDeleted && p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }
}

