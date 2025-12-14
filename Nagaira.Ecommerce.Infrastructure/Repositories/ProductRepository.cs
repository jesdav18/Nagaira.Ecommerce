using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Prices)
                .ThenInclude(pp => pp.PriceLevel)
            .Include(p => p.InventoryBalance)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Prices)
                .ThenInclude(pp => pp.PriceLevel)
            .Include(p => p.InventoryBalance)
            .Where(p => !p.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Prices)
                .ThenInclude(pp => pp.PriceLevel)
            .Include(p => p.InventoryBalance)
            .Where(p => p.CategoryId == categoryId && p.IsActive && !p.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Prices)
                .ThenInclude(pp => pp.PriceLevel)
            .Include(p => p.InventoryBalance)
            .Where(p => p.IsActive && !p.IsDeleted)
            .ToListAsync();
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Prices)
                .ThenInclude(pp => pp.PriceLevel)
            .Include(p => p.InventoryBalance)
            .FirstOrDefaultAsync(p => p.Sku == sku && !p.IsDeleted);
    }

    public async Task<Product?> GetBySkuIncludingDeletedAsync(string sku)
    {
        // Usar IgnoreQueryFilters para asegurar que buscamos todos los productos, incluso eliminados
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Sku == sku);
    }

    public async Task<bool> SkuExistsAsync(string sku)
    {
        // Verificar si el SKU existe (sin filtros)
        return await _dbSet
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Sku == sku && !p.IsDeleted);
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Prices)
                .ThenInclude(pp => pp.PriceLevel)
            .Include(p => p.InventoryBalance)
            .Where(p => (p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm)) 
                && p.IsActive && !p.IsDeleted)
            .ToListAsync();
    }
}
