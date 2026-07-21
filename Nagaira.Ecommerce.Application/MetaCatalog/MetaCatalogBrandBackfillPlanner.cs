using System.Globalization;
using System.Text;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.MetaCatalog;

public static class MetaCatalogBrandBackfillPlanner
{
    private static readonly IReadOnlyList<KnownBrand> KnownBrands =
    [
        new("Head & Shoulders"),
        new("Old Spice"),
        new("L'Oréal"),
        new("Johnson's"),
        new("Axe"),
        new("Rexona"),
        new("Dove"),
        new("Nivea"),
        new("Lubriderm"),
        new("Mennen"),
        new("Pantene"),
        new("Colgate"),
        new("Palmolive"),
        new("Gillette"),
        new("Garnier"),
        new("Neutrogena"),
        new("Vaseline"),
        new("Pond's"),
        new("Suave"),
        new("Tresemmé"),
        new("Kotex"),
        new("Always"),
        new("Huggies"),
        new("Pampers")
    ];

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
            return CreateItem(product, currentBrand, null, MetaCatalogBrandBackfillPlanOperations.Unchanged, MetaCatalogBrandBackfillConfidence.High, "brand_already_set");
        }

        var brandFromProductName = FindKnownBrandInProductName(product.Name);
        if (brandFromProductName != null)
        {
            return CreateItem(product, null, brandFromProductName, MetaCatalogBrandBackfillPlanOperations.Update, MetaCatalogBrandBackfillConfidence.High, "product_name_contains_brand");
        }

        suppliersByProductId.TryGetValue(product.Id, out var suppliers);
        var activeSuppliers = suppliers?
            .Where(ps => ps.IsActive && !ps.IsDeleted && ps.Supplier != null && ps.Supplier.IsActive && !ps.Supplier.IsDeleted)
            .OrderByDescending(ps => ps.IsPrimary)
            .ThenBy(ps => ps.Priority)
            .ThenBy(ps => ps.SupplierId)
            .ToList() ?? [];

        foreach (var supplier in activeSuppliers)
        {
            var brandFromSupplier = FindKnownBrandByExactName(supplier.Supplier.Name);
            if (brandFromSupplier != null)
            {
                return CreateItem(product, null, brandFromSupplier, MetaCatalogBrandBackfillPlanOperations.Update, MetaCatalogBrandBackfillConfidence.High, "supplier_matches_known_brand");
            }
        }

        return CreateItem(product, null, null, MetaCatalogBrandBackfillPlanOperations.Skipped, MetaCatalogBrandBackfillConfidence.None, "brand_not_recognized");
    }

    private static string? NormalizeBrand(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string? FindKnownBrandInProductName(string productName)
    {
        var normalizedProductName = NormalizeForMatch(productName);
        if (normalizedProductName.Length == 0)
        {
            return null;
        }

        return KnownBrands.FirstOrDefault(brand => normalizedProductName.Contains(brand.MatchKey, StringComparison.Ordinal))?.Name;
    }

    private static string? FindKnownBrandByExactName(string supplierName)
    {
        var normalizedSupplierName = NormalizeForMatch(supplierName);
        if (normalizedSupplierName.Length == 0)
        {
            return null;
        }

        return KnownBrands.FirstOrDefault(brand => string.Equals(normalizedSupplierName, brand.MatchKey, StringComparison.Ordinal))?.Name;
    }

    private static string NormalizeForMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static MetaCatalogBrandBackfillPlanItem CreateItem(
        Product product,
        string? currentBrand,
        string? suggestedBrand,
        string operation,
        string confidence,
        string? reason)
    {
        return new MetaCatalogBrandBackfillPlanItem(
            product.Id,
            product.Name,
            product.Sku,
            currentBrand,
            suggestedBrand,
            operation,
            confidence,
            reason);
    }

    private sealed record KnownBrand(string Name)
    {
        public string MatchKey { get; } = NormalizeForMatch(Name);
    }
}

public static class MetaCatalogBrandBackfillPlanOperations
{
    public const string Update = "UPDATE";
    public const string Unchanged = "UNCHANGED";
    public const string Skipped = "SKIPPED";
}

public static class MetaCatalogBrandBackfillConfidence
{
    public const string High = "HIGH";
    public const string None = "NONE";
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
    string Confidence,
    string? Reason);
