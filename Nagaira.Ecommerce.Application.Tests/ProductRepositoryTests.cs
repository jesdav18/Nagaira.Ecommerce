using Microsoft.EntityFrameworkCore;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Infrastructure.Data;
using Nagaira.Ecommerce.Infrastructure.Repositories;

namespace Nagaira.Ecommerce.Application.Tests;

public class ProductRepositoryTests
{
    [Fact]
    public async Task GetMetaCatalogSyncPlanCandidates_DoesNotThrowWhenProductUpdatedAtIsNotMapped()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var context = new ApplicationDbContext(options);
        Assert.Null(context.Model.FindEntityType(typeof(Product))?.FindProperty(nameof(Product.UpdatedAt)));

        var priceLevelId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Redes",
            Slug = "redes",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var priceLevel = new PriceLevel
        {
            Id = priceLevelId,
            Name = "Retail",
            Priority = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var product = new Product
        {
            Id = productId,
            Name = "Router WiFi",
            Description = "Router para casa",
            Brand = "Acme",
            Sku = "RTR-001",
            Slug = "router-wifi",
            CategoryId = category.Id,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);
        context.PriceLevels.Add(priceLevel);
        context.Products.Add(product);
        context.ProductImages.Add(new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ImageUrl = "https://cdn.example/router.jpg",
            IsPrimary = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });
        context.ProductPrices.Add(new ProductPrice
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            PriceLevelId = priceLevelId,
            Price = 82.50m,
            MinQuantity = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });
        context.InventoryBalances.Add(new InventoryBalance
        {
            ProductId = productId,
            AvailableQuantity = 10,
            ReservedQuantity = 0,
            LastUpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        var result = await repository.GetMetaCatalogSyncPlanCandidatesAsync(50);

        var item = Assert.Single(result);
        Assert.Equal(productId, item.Id);
    }
}
