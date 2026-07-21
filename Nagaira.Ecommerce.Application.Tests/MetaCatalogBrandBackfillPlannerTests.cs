using Nagaira.Ecommerce.Application.MetaCatalog;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.Tests;

public class MetaCatalogBrandBackfillPlannerTests
{
    [Fact]
    public void BuildPlan_ProductNameWithKnownBrandReturnsUpdate()
    {
        var product = CreateProduct(name: "Desodorante Rexona Clinical");

        var plan = BuildPlan([product], []);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Update, item.Operation);
        Assert.Equal("Rexona", item.SuggestedBrand);
        Assert.Equal(MetaCatalogBrandBackfillConfidence.High, item.Confidence);
        Assert.Equal("product_name_contains_brand", item.Reason);
        Assert.Equal(1, plan.Summary.Update);
    }

    [Fact]
    public void BuildPlan_ProductNamePrefersSpecificCompositeBrands()
    {
        var product = CreateProduct(name: "Shampoo Head & Shoulders Old Spice 400ml");

        var plan = BuildPlan([product], []);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Update, item.Operation);
        Assert.Equal("Head & Shoulders", item.SuggestedBrand);
        Assert.Equal("product_name_contains_brand", item.Reason);
    }

    [Fact]
    public void BuildPlan_ProductWithBrandReturnsUnchanged()
    {
        var product = CreateProduct();
        product.Brand = "Acme";

        var plan = BuildPlan([product], []);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Unchanged, item.Operation);
        Assert.Equal("Acme", item.CurrentBrand);
        Assert.Null(item.SuggestedBrand);
        Assert.Equal(MetaCatalogBrandBackfillConfidence.High, item.Confidence);
        Assert.Equal("brand_already_set", item.Reason);
    }

    [Fact]
    public void BuildPlan_ProductWithoutRecognizedBrandReturnsSkipped()
    {
        var product = CreateProduct();

        var plan = BuildPlan([product], []);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Skipped, item.Operation);
        Assert.Equal(MetaCatalogBrandBackfillConfidence.None, item.Confidence);
        Assert.Equal("brand_not_recognized", item.Reason);
    }

    [Fact]
    public void BuildPlan_ProductWithSupplierExactKnownBrandReturnsUpdate()
    {
        var product = CreateProduct();
        var supplier = CreateProductSupplier(product.Id, " Dove ");

        var plan = BuildPlan([product], [supplier]);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Update, item.Operation);
        Assert.Equal("Dove", item.SuggestedBrand);
        Assert.Equal(MetaCatalogBrandBackfillConfidence.High, item.Confidence);
        Assert.Equal("supplier_matches_known_brand", item.Reason);
    }

    [Fact]
    public void BuildPlan_GenericSupplierNameNeverBecomesBrand()
    {
        var product = CreateProduct();
        var supplier = CreateProductSupplier(product.Id, "Distribuidora Central", isPrimary: true);

        var plan = BuildPlan([product], [supplier]);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Skipped, item.Operation);
        Assert.Null(item.SuggestedBrand);
        Assert.Equal(MetaCatalogBrandBackfillConfidence.None, item.Confidence);
        Assert.Equal("brand_not_recognized", item.Reason);
    }

    [Fact]
    public void BuildPlan_SupplierNameContainingKnownBrandButNotExactReturnsSkipped()
    {
        var product = CreateProduct();
        var supplier = CreateProductSupplier(product.Id, "Distribuidora Rexona");

        var plan = BuildPlan([product], [supplier]);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Skipped, item.Operation);
        Assert.Null(item.SuggestedBrand);
        Assert.Equal("brand_not_recognized", item.Reason);
    }

    [Fact]
    public void BuildPlan_ProductWithBlankSupplierNameReturnsSkippedAsUnrecognized()
    {
        var product = CreateProduct();
        var supplier = CreateProductSupplier(product.Id, " ");

        var plan = BuildPlan([product], [supplier]);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Skipped, item.Operation);
        Assert.Equal("brand_not_recognized", item.Reason);
    }

    [Fact]
    public void BuildPlan_ProductNameMatchingAccentInsensitiveReturnsCanonicalBrand()
    {
        var product = CreateProduct(name: "Crema LOREAL hidratante");

        var plan = BuildPlan([product], []);

        var item = Assert.Single(plan.Items);
        Assert.Equal(MetaCatalogBrandBackfillPlanOperations.Update, item.Operation);
        Assert.Equal("L'Oréal", item.SuggestedBrand);
        Assert.Equal("product_name_contains_brand", item.Reason);
    }

    [Fact]
    public void BuildPlan_LimitIsCappedAtTwoHundred()
    {
        var products = Enumerable.Range(1, 250)
            .Select(i => CreateProduct(Guid.Parse($"11111111-1111-1111-1111-{i:000000000000}"), DateTime.UtcNow.AddMinutes(i)))
            .ToList();

        var plan = MetaCatalogBrandBackfillPlanner.BuildPlan(products, new Dictionary<Guid, IReadOnlyCollection<ProductSupplier>>(), 500);

        Assert.Equal(200, plan.Limit);
        Assert.Equal(200, plan.Items.Count);
        Assert.Equal(200, plan.Summary.Scanned);
    }

    private static MetaCatalogBrandBackfillPlanResponse BuildPlan(
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<ProductSupplier> suppliers)
    {
        return MetaCatalogBrandBackfillPlanner.BuildPlan(
            products,
            suppliers
                .GroupBy(ps => ps.ProductId)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<ProductSupplier>)g.ToList()),
            200);
    }

    private static Product CreateProduct(Guid? id = null, DateTime? createdAt = null, string name = "Desodorante")
    {
        var productId = id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        return new Product
        {
            Id = productId,
            Name = name,
            Sku = $"SKU-{productId.ToString("N")[..6]}",
            Brand = null,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    private static ProductSupplier CreateProductSupplier(Guid productId, string supplierName, bool isPrimary = false)
    {
        var supplierId = Guid.NewGuid();
        return new ProductSupplier
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            SupplierId = supplierId,
            IsActive = true,
            IsDeleted = false,
            IsPrimary = isPrimary,
            Priority = 1,
            Supplier = new Supplier
            {
                Id = supplierId,
                Name = supplierName,
                IsActive = true,
                IsDeleted = false
            }
        };
    }
}
