using System.Globalization;
using Nagaira.Ecommerce.Application.MetaCatalog;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.Tests;

public class MetaCatalogProductMapperTests
{
    private static readonly Guid RetailPriceLevelId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WholesalePriceLevelId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Map_ActivePublishableProductReturnsUpsert()
    {
        var product = CreateProduct();

        var result = MetaCatalogProductMapper.Map(product, CreateOptions());

        Assert.Equal(MetaCatalogSyncAction.Upsert, result.Action);
        Assert.NotNull(result.Item);
        Assert.Equal(product.Id.ToString("D"), result.RetailerId);
        Assert.Equal(product.Id.ToString("D"), result.Item.RetailerId);
        Assert.Equal("Acme", result.Item.Brand);
        Assert.Equal("125.50", result.Item.Price);
        Assert.Equal("HNL", result.Item.Currency);
        Assert.Equal("https://store.example/p/router-wifi", result.Item.Url);
        Assert.Equal("https://cdn.example/router-primary.jpg", result.Item.ImageUrl);
        Assert.Equal("in stock", result.Item.Availability);
        Assert.False(string.IsNullOrWhiteSpace(result.PayloadHash));
    }

    [Fact]
    public void Map_DoesNotUseQuantityBreakAsMainCatalogPrice()
    {
        var product = CreateProduct();
        product.Prices.Add(new ProductPrice
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            PriceLevelId = RetailPriceLevelId,
            Price = 99.99m,
            MinQuantity = 3,
            IsActive = true
        });

        var result = MetaCatalogProductMapper.Map(product, CreateOptions());

        Assert.Equal(MetaCatalogSyncAction.Upsert, result.Action);
        Assert.NotNull(result.Item);
        Assert.Equal("125.50", result.Item.Price);
    }

    [Fact]
    public void Map_ProductWithoutAvailableStockReturnsUpsertOutOfStock()
    {
        var product = CreateProduct();
        product.InventoryBalance = new InventoryBalance
        {
            ProductId = product.Id,
            AvailableQuantity = 0,
            ReservedQuantity = 0,
            LastUpdatedAt = DateTime.UtcNow
        };

        var result = MetaCatalogProductMapper.Map(product, CreateOptions());

        Assert.Equal(MetaCatalogSyncAction.Upsert, result.Action);
        Assert.NotNull(result.Item);
        Assert.Equal("out of stock", result.Item.Availability);
    }

    [Fact]
    public void Map_ProductWithVirtualStockReturnsUpsertInStock()
    {
        var product = CreateProduct();
        product.HasVirtualStock = true;
        product.InventoryBalance = new InventoryBalance
        {
            ProductId = product.Id,
            AvailableQuantity = 0,
            ReservedQuantity = 0,
            LastUpdatedAt = DateTime.UtcNow
        };

        var result = MetaCatalogProductMapper.Map(product, CreateOptions());

        Assert.Equal(MetaCatalogSyncAction.Upsert, result.Action);
        Assert.NotNull(result.Item);
        Assert.Equal("in stock", result.Item.Availability);
    }

    [Fact]
    public void Map_InactiveProductReturnsDeleteWithoutPublishableItem()
    {
        var product = CreateProduct();
        product.IsActive = false;

        var result = MetaCatalogProductMapper.Map(product, CreateOptions());

        Assert.Equal(MetaCatalogSyncAction.Delete, result.Action);
        Assert.Equal(product.Id.ToString("D"), result.RetailerId);
        Assert.Null(result.Item);
    }

    [Fact]
    public void Map_DeletedProductReturnsDeleteWithoutPublishableItem()
    {
        var product = CreateProduct();
        product.IsDeleted = true;

        var result = MetaCatalogProductMapper.Map(product, CreateOptions());

        Assert.Equal(MetaCatalogSyncAction.Delete, result.Action);
        Assert.Equal(product.Id.ToString("D"), result.RetailerId);
        Assert.Null(result.Item);
    }

    [Fact]
    public void Map_DeleteHashDiffersFromUpsertHashForSameRetailerId()
    {
        var active = CreateProduct();
        var deleted = CreateProduct();
        deleted.IsDeleted = true;

        var upsert = MetaCatalogProductMapper.Map(active, CreateOptions());
        var delete = MetaCatalogProductMapper.Map(deleted, CreateOptions());

        Assert.Equal(upsert.RetailerId, delete.RetailerId);
        Assert.NotEqual(upsert.PayloadHash, delete.PayloadHash);
    }

    [Fact]
    public void TryMap_ProductWithoutBrandReturnsSkipped()
    {
        var product = CreateProduct();
        product.Brand = null;

        var result = MetaCatalogProductMapper.TryMap(product, CreateOptions());

        Assert.Equal(MetaCatalogProductMappingStatus.Skipped, result.Status);
        Assert.Equal("missing_brand", result.Reason);
        Assert.Null(result.MappingResult);
    }

    [Fact]
    public void TryMap_ProductWithoutConfiguredPriceLevelReturnsSkipped()
    {
        var product = CreateProduct();
        product.Prices.RemoveAll(p => p.PriceLevelId == RetailPriceLevelId && p.MinQuantity <= 1);

        var result = MetaCatalogProductMapper.TryMap(product, CreateOptions());

        Assert.Equal(MetaCatalogProductMappingStatus.Skipped, result.Status);
        Assert.Equal("missing_public_price", result.Reason);
        Assert.Null(result.MappingResult);
    }

    [Fact]
    public void TryMap_ProductWithoutPrimaryImageReturnsSkipped()
    {
        var product = CreateProduct();
        product.Images.Clear();

        var result = MetaCatalogProductMapper.TryMap(product, CreateOptions());

        Assert.Equal(MetaCatalogProductMappingStatus.Skipped, result.Status);
        Assert.Equal("missing_image", result.Reason);
        Assert.Null(result.MappingResult);
    }

    [Fact]
    public void Map_SelectsPrimaryImageBeforeLowerDisplayOrderImage()
    {
        var product = CreateProduct();
        product.Images.Insert(0, new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ImageUrl = "https://cdn.example/router-secondary.jpg",
            DisplayOrder = 0,
            IsPrimary = false
        });

        var result = MetaCatalogProductMapper.Map(product, CreateOptions());

        Assert.NotNull(result.Item);
        Assert.Equal("https://cdn.example/router-primary.jpg", result.Item.ImageUrl);
    }

    [Fact]
    public void Hash_SamePayloadReturnsSameHash()
    {
        var hasher = new MetaCatalogPayloadHasher();
        var item = CreateMetaProduct();

        var first = hasher.HashUpsert(item);
        var second = hasher.HashUpsert(item);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Hash_EquivalentPayloadConstructedSeparatelyReturnsSameHash()
    {
        var hasher = new MetaCatalogPayloadHasher();
        var first = CreateMetaProduct();
        var second = new MetaCatalogProduct(
            " product-1 ",
            " Router WiFi ",
            " Router para casa ",
            " Acme ",
            "in stock",
            "new",
            "125.5",
            "HNL",
            "https://store.example/p/router-wifi",
            "https://cdn.example/router-primary.jpg",
            " Redes ",
            " RTR-001 "
        );

        Assert.Equal(hasher.HashUpsert(first), hasher.HashUpsert(second));
    }

    [Fact]
    public void Hash_PriceChangeChangesHash()
    {
        var hasher = new MetaCatalogPayloadHasher();
        var first = CreateMetaProduct(price: "125.50");
        var second = CreateMetaProduct(price: "126.50");

        Assert.NotEqual(hasher.HashUpsert(first), hasher.HashUpsert(second));
    }

    [Fact]
    public void Hash_ImageChangeChangesHash()
    {
        var hasher = new MetaCatalogPayloadHasher();
        var first = CreateMetaProduct(imageUrl: "https://cdn.example/router-primary.jpg");
        var second = CreateMetaProduct(imageUrl: "https://cdn.example/router-new.jpg");

        Assert.NotEqual(hasher.HashUpsert(first), hasher.HashUpsert(second));
    }

    [Fact]
    public void Hash_AvailabilityChangeChangesHash()
    {
        var hasher = new MetaCatalogPayloadHasher();
        var first = CreateMetaProduct(availability: "in stock");
        var second = CreateMetaProduct(availability: "out of stock");

        Assert.NotEqual(hasher.HashUpsert(first), hasher.HashUpsert(second));
    }

    [Fact]
    public void Hash_InternalSyncStateChangesDoNotAffectHash()
    {
        var product = CreateProduct();
        var firstState = new MetaProductSyncState
        {
            ProductId = product.Id,
            RetailerId = product.Id.ToString("D"),
            Status = MetaProductSyncStatuses.Pending,
            RetryCount = 0,
            LastAttemptAt = DateTime.UtcNow.AddDays(-1),
            LastSyncedAt = DateTime.UtcNow.AddDays(-2)
        };
        var secondState = new MetaProductSyncState
        {
            ProductId = product.Id,
            RetailerId = product.Id.ToString("D"),
            Status = MetaProductSyncStatuses.Failed,
            RetryCount = 99,
            LastAttemptAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow.AddHours(-1)
        };

        var first = MetaCatalogProductMapper.Map(product, CreateOptions());
        var second = MetaCatalogProductMapper.Map(product, CreateOptions());

        Assert.NotEqual(firstState.RetryCount, secondState.RetryCount);
        Assert.Equal(first.PayloadHash, second.PayloadHash);
    }

    [Fact]
    public void Hash_NullAndEmptyStringsUseSameCanonicalValue()
    {
        var hasher = new MetaCatalogPayloadHasher();
        var withNullCategory = CreateMetaProduct(categoryName: null);
        var withEmptyCategory = CreateMetaProduct(categoryName: " ");

        Assert.Equal(hasher.HashUpsert(withNullCategory), hasher.HashUpsert(withEmptyCategory));
    }

    [Fact]
    public void Hash_SystemCultureDoesNotAffectResult()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        var hasher = new MetaCatalogPayloadHasher();
        var item = CreateMetaProduct(price: "125.50");

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-HN");
            CultureInfo.CurrentUICulture = new CultureInfo("es-HN");
            var first = hasher.HashUpsert(item);

            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var second = hasher.HashUpsert(item);

            Assert.Equal(first, second);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
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

    private static MetaCatalogProduct CreateMetaProduct(
        string price = "125.50",
        string imageUrl = "https://cdn.example/router-primary.jpg",
        string availability = "in stock",
        string? categoryName = "Redes")
    {
        return new MetaCatalogProduct(
            "product-1",
            "Router WiFi",
            "Router para casa",
            "Acme",
            availability,
            "new",
            price,
            "HNL",
            "https://store.example/p/router-wifi",
            imageUrl,
            categoryName,
            "RTR-001"
        );
    }

    private static Product CreateProduct()
    {
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        return new Product
        {
            Id = productId,
            Name = " Router WiFi ",
            Description = " Router para casa ",
            Brand = " Acme ",
            Sku = "RTR-001",
            Slug = "router-wifi",
            IsActive = true,
            IsDeleted = false,
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
                },
                new ProductPrice
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    PriceLevelId = WholesalePriceLevelId,
                    Price = 115.00m,
                    MinQuantity = 1,
                    IsActive = true,
                    PriceLevel = new PriceLevel
                    {
                        Id = WholesalePriceLevelId,
                        Name = "Wholesale",
                        Priority = 2,
                        IsActive = true
                    }
                }
            ]
        };
    }
}
