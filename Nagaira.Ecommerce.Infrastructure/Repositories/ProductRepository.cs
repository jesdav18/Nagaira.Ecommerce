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
        var categoryRepository = _context.Set<Category>();
        var allCategories = await categoryRepository
            .Where(c => c.IsActive && !c.IsDeleted)
            .ToListAsync();
        
        var categoryIds = new List<Guid> { categoryId };
        
        void CollectChildren(Guid parentId)
        {
            var children = allCategories.Where(c => c.ParentCategoryId == parentId).ToList();
            foreach (var child in children)
            {
                categoryIds.Add(child.Id);
                CollectChildren(child.Id);
            }
        }
        
        CollectChildren(categoryId);
        
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Prices)
                .ThenInclude(pp => pp.PriceLevel)
            .Include(p => p.InventoryBalance)
            .Where(p => categoryIds.Contains(p.CategoryId) && p.IsActive && !p.IsDeleted)
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
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Product>();

        var searchPattern = $"%{searchTerm}%";
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Prices)
                .ThenInclude(pp => pp.PriceLevel)
            .Include(p => p.InventoryBalance)
            .Where(p => (EF.Functions.ILike(p.Name, searchPattern) || 
                        (p.Description != null && EF.Functions.ILike(p.Description, searchPattern)) ||
                        EF.Functions.ILike(p.Sku, searchPattern))
                && p.IsActive && !p.IsDeleted)
            .ToListAsync();
    }
}
