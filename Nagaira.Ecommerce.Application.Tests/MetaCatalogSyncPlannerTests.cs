using Nagaira.Ecommerce.Application.MetaCatalog;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.Tests;

public class MetaCatalogSyncPlannerTests
{
    private static readonly Guid RetailPriceLevelId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void BuildPlan_NewProductReturnsCreate()
    {
        var product = CreateProduct();

        var plan = BuildPlan([product], []);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogSyncPlanOperations.Create, item.Operation);
        Assert.Equal(1, plan.Summary.Create);
    }

    [Fact]
    public void BuildPlan_SameSuccessfulHashReturnsUnchanged()
    {
        var product = CreateProduct();
        var mapping = MetaCatalogProductMapper.Map(product, CreateOptions());
        var state = SyncedState(product, mapping.PayloadHash);

        var plan = BuildPlan([product], [state]);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogSyncPlanOperations.Unchanged, item.Operation);
        Assert.Equal(mapping.PayloadHash, item.PreviousPayloadHash);
    }

    [Fact]
    public void BuildPlan_DifferentSuccessfulHashReturnsUpdate()
    {
        var product = CreateProduct();
        var state = SyncedState(product, "previous-hash");

        var plan = BuildPlan([product], [state]);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogSyncPlanOperations.Update, item.Operation);
        Assert.NotEqual(item.PayloadHash, item.PreviousPayloadHash);
    }

    [Fact]
    public void BuildPlan_InactiveProductWithPreviousSyncReturnsDelete()
    {
        var product = CreateProduct();
        product.IsActive = false;
        var state = SyncedState(product, "previous-upsert-hash");

        var plan = BuildPlan([product], [state]);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogSyncPlanOperations.Delete, item.Operation);
    }

    [Fact]
    public void BuildPlan_PreviouslyDeletedProductReturnsAlreadyDeleted()
    {
        var product = CreateProduct();
        product.IsDeleted = true;
        var deleteHash = new MetaCatalogPayloadHasher().HashDelete(product.Id.ToString("D"));
        var state = SyncedState(product, deleteHash);

        var plan = BuildPlan([product], [state]);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogSyncPlanOperations.AlreadyDeleted, item.Operation);
    }

    [Fact]
    public void BuildPlan_ProductWithoutBrandReturnsSkipped()
    {
        var product = CreateProduct();
        product.Brand = null;

        var plan = BuildPlan([product], []);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogSyncPlanOperations.Skipped, item.Operation);
        Assert.Equal("missing_brand", item.Reason);
    }

    [Fact]
    public void BuildPlan_ProductWithoutImageReturnsSkipped()
    {
        var product = CreateProduct();
        product.Images.Clear();

        var plan = BuildPlan([product], []);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogSyncPlanOperations.Skipped, item.Operation);
        Assert.Equal("missing_image", item.Reason);
    }

    [Fact]
    public void BuildPlan_ProductWithoutPublicPriceReturnsSkipped()
    {
        var product = CreateProduct();
        product.Prices.Clear();

        var plan = BuildPlan([product], []);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogSyncPlanOperations.Skipped, item.Operation);
        Assert.Equal("missing_public_price", item.Reason);
    }

    [Fact]
    public void BuildPlan_IncompleteInactiveProductNeverReturnsDelete()
    {
        var product = CreateProduct();
        product.IsActive = false;
        product.Brand = null;
        var state = SyncedState(product, "previous-upsert-hash");

        var plan = BuildPlan([product], [state]);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogSyncPlanOperations.Skipped, item.Operation);
        Assert.Equal("missing_brand", item.Reason);
    }

    [Fact]
    public void BuildPlan_LimitIsCappedAtTwoHundred()
    {
        var products = Enumerable.Range(1, 250)
            .Select(i => CreateProduct(Guid.Parse($"11111111-1111-1111-1111-{i:000000000000}"), updatedAt: DateTime.UtcNow.AddMinutes(i)))
            .ToList();

        var plan = MetaCatalogSyncPlanner.BuildPlan(products, new Dictionary<Guid, MetaProductSyncState>(), CreateOptions(), 500);

        Assert.Equal(200, plan.Limit);
        Assert.Equal(200, plan.Items.Count);
        Assert.Equal(200, plan.Summary.Scanned);
    }

    private static MetaCatalogSyncPlanResponse BuildPlan(
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<MetaProductSyncState> states)
    {
        return MetaCatalogSyncPlanner.BuildPlan(
            products,
            states.ToDictionary(s => s.ProductId),
            CreateOptions(),
            50);
    }

    private static MetaProductSyncState SyncedState(Product product, string payloadHash)
    {
        return new MetaProductSyncState
        {
            ProductId = product.Id,
            RetailerId = product.Id.ToString("D"),
            Status = MetaProductSyncStatuses.Synced,
            LastPayloadHash = payloadHash,
            LastSyncedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    private static MetaCatalogOptions CreateOptions()
    {
        return new MetaCatalogOptions
        {
            Currency = "HNL",
            PublicBaseUrl = "https://store.example",
            PublicPriceLevelId = RetailPriceLevelId
        };
    }

    private static Product CreateProduct(Guid? id = null, DateTime? updatedAt = null)
    {
        var productId = id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");

        return new Product
        {
            Id = productId,
            Name = "Router WiFi",
            Description = "Router para casa",
            Brand = "Acme",
            Sku = $"RTR-{productId.ToString("N")[..6]}",
            Slug = $"router-{productId.ToString("N")[..6]}",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = updatedAt ?? DateTime.UtcNow,
            CategoryId = Guid.NewGuid(),
            Category = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Redes",
                Slug = "redes",
                IsActive = true
            },
            InventoryBalance = new InventoryBalance
            {
                ProductId = productId,
                AvailableQuantity = 5,
                ReservedQuantity = 0,
                LastUpdatedAt = DateTime.UtcNow
            },
            Images =
            [
                new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    ImageUrl = "https://cdn.example/router-primary.jpg",
                    DisplayOrder = 10,
                    IsPrimary = true
                }
            ],
            Prices =
            [
                new ProductPrice
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    PriceLevelId = RetailPriceLevelId,
                    Price = 125.50m,
                    MinQuantity = 1,
                    IsActive = true,
                    PriceLevel = new PriceLevel
                    {
                        Id = RetailPriceLevelId,
                        Name = "Retail",
                        Priority = 1,
                        IsActive = true
                    }
                }
            ]
        };
    }
}
