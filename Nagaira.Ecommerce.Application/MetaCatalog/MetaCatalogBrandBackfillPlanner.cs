using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.MetaCatalog;

public static class MetaCatalogBrandBackfillPlanner
{
    public static MetaCatalogBrandBackfillPlanResponse BuildPlan(
        IReadOnlyCollection<Product> products,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<ProductSupplier>> suppliersByProductId,
        int limit)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var items = products
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(safeLimit)
            .Select(product => BuildItem(product, suppliersByProductId))
            .ToList();

        return new MetaCatalogBrandBackfillPlanResponse(
            true,
            safeLimit,
            MetaCatalogBrandBackfillPlanSummary.FromItems(items),
            items);
    }

    private static MetaCatalogBrandBackfillPlanItem BuildItem(
        Product product,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<ProductSupplier>> suppliersByProductId)
    {
        var currentBrand = NormalizeBrand(product.Brand);
        if (currentBrand != null)
        {
            return CreateItem(product, currentBrand, null, MetaCatalogBrandBackfillPlanOperations.Unchanged, "brand_already_set");
        }

        suppliersByProductId.TryGetValue(product.Id, out var suppliers);
        var activeSuppliers = suppliers?
            .Where(ps => ps.IsActive && !ps.IsDeleted && ps.Supplier != null && ps.Supplier.IsActive && !ps.Supplier.IsDeleted)
            .OrderByDescending(ps => ps.IsPrimary)
            .ThenBy(ps => ps.Priority)
            .ThenBy(ps => ps.SupplierId)
            .ToList() ?? [];

        if (activeSuppliers.Count == 0)
        {
            return CreateItem(product, null, null, MetaCatalogBrandBackfillPlanOperations.Skipped, "missing_supplier");
        }

        var primarySuppliers = activeSuppliers.Where(ps => ps.IsPrimary).ToList();
        ProductSupplier? selectedSupplier = null;
        string reason;

        if (primarySuppliers.Count == 1)
        {
            selectedSupplier = primarySuppliers[0];
            reason = "primary_supplier";
        }
        else if (primarySuppliers.Count > 1)
        {
            selectedSupplier = primarySuppliers
                .OrderBy(ps => ps.Priority)
                .ThenBy(ps => ps.SupplierId)
                .First();
            reason = "primary_supplier";
        }
        else if (activeSuppliers.Count == 1)
        {
            selectedSupplier = activeSuppliers[0];
            reason = "single_supplier";
        }
        else
        {
            return CreateItem(product, null, null, MetaCatalogBrandBackfillPlanOperations.Skipped, "ambiguous_suppliers");
        }

        var suggestedBrand = NormalizeBrand(selectedSupplier.Supplier.Name);
        if (suggestedBrand == null)
        {
            return CreateItem(product, null, null, MetaCatalogBrandBackfillPlanOperations.Skipped, "missing_supplier_name");
        }

        if (suggestedBrand.Length > 255)
        {
            return CreateItem(product, null, null, MetaCatalogBrandBackfillPlanOperations.Skipped, "brand_too_long");
        }

        return CreateItem(product, null, suggestedBrand, MetaCatalogBrandBackfillPlanOperations.Update, reason);
    }

    private static string? NormalizeBrand(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static MetaCatalogBrandBackfillPlanItem CreateItem(
        Product product,
        string? currentBrand,
        string? suggestedBrand,
        string operation,
        string? reason)
    {
        return new MetaCatalogBrandBackfillPlanItem(
            product.Id,
            product.Name,
            product.Sku,
            currentBrand,
            suggestedBrand,
            operation,
            reason);
    }
}

public static class MetaCatalogBrandBackfillPlanOperations
{
    public const string Update = "UPDATE";
    public const string Unchanged = "UNCHANGED";
    public const string Skipped = "SKIPPED";
}

public record MetaCatalogBrandBackfillPlanResponse(
    bool DryRun,
    int Limit,
    MetaCatalogBrandBackfillPlanSummary Summary,
    IReadOnlyList<MetaCatalogBrandBackfillPlanItem> Items);

public record MetaCatalogBrandBackfillPlanSummary(
    int Scanned,
    int Update,
    int Unchanged,
    int Skipped)
{
    public static MetaCatalogBrandBackfillPlanSummary FromItems(IReadOnlyCollection<MetaCatalogBrandBackfillPlanItem> items)
    {
        return new MetaCatalogBrandBackfillPlanSummary(
            items.Count,
            Count(items, MetaCatalogBrandBackfillPlanOperations.Update),
            Count(items, MetaCatalogBrandBackfillPlanOperations.Unchanged),
            Count(items, MetaCatalogBrandBackfillPlanOperations.Skipped));
    }

    private static int Count(IEnumerable<MetaCatalogBrandBackfillPlanItem> items, string operation)
    {
        return items.Count(i => string.Equals(i.Operation, operation, StringComparison.Ordinal));
    }
}

public record MetaCatalogBrandBackfillPlanItem(
    Guid ProductId,
    string Name,
    string Sku,
    string? CurrentBrand,
    string? SuggestedBrand,
    string Operation,
    string? Reason);
