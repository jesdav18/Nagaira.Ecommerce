using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.MetaCatalog;

public static class MetaCatalogSyncPlanner
{
    public static MetaCatalogSyncPlanResponse BuildPlan(
        IReadOnlyCollection<Product> products,
        IReadOnlyDictionary<Guid, MetaProductSyncState> syncStatesByProductId,
        MetaCatalogOptions options,
        int limit)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var items = products
            .OrderBy(p => p.UpdatedAt ?? p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(safeLimit)
            .Select(product => BuildItem(product, syncStatesByProductId, options))
            .ToList();

        return new MetaCatalogSyncPlanResponse(
            true,
            safeLimit,
            MetaCatalogSyncPlanSummary.FromItems(items),
            items);
    }

    private static MetaCatalogSyncPlanItem BuildItem(
        Product product,
        IReadOnlyDictionary<Guid, MetaProductSyncState> syncStatesByProductId,
        MetaCatalogOptions options)
    {
        syncStatesByProductId.TryGetValue(product.Id, out var state);
        var outcome = MetaCatalogProductMapper.TryMap(product, options);
        var previousPayloadHash = state?.LastPayloadHash;

        if (outcome.Status == MetaCatalogProductMappingStatus.Skipped)
        {
            return CreateItem(
                product,
                outcome.RetailerId,
                MetaCatalogSyncPlanOperations.Skipped,
                null,
                previousPayloadHash,
                outcome.Reason);
        }

        var mapping = outcome.MappingResult!;
        if (mapping.Action == MetaCatalogSyncAction.Delete)
        {
            if (!HasSuccessfulPayloadHash(state))
            {
                return CreateItem(
                    product,
                    mapping.RetailerId,
                    MetaCatalogSyncPlanOperations.Skipped,
                    mapping.PayloadHash,
                    previousPayloadHash,
                    "no_previous_meta_sync");
            }

            if (string.Equals(previousPayloadHash, mapping.PayloadHash, StringComparison.Ordinal))
            {
                return CreateItem(
                    product,
                    mapping.RetailerId,
                    MetaCatalogSyncPlanOperations.AlreadyDeleted,
                    mapping.PayloadHash,
                    previousPayloadHash,
                    null);
            }

            return CreateItem(
                product,
                mapping.RetailerId,
                MetaCatalogSyncPlanOperations.Delete,
                mapping.PayloadHash,
                previousPayloadHash,
                null);
        }

        if (!HasSuccessfulPayloadHash(state))
        {
            return CreateItem(
                product,
                mapping.RetailerId,
                MetaCatalogSyncPlanOperations.Create,
                mapping.PayloadHash,
                previousPayloadHash,
                null);
        }

        if (string.Equals(previousPayloadHash, mapping.PayloadHash, StringComparison.Ordinal))
        {
            return CreateItem(
                product,
                mapping.RetailerId,
                MetaCatalogSyncPlanOperations.Unchanged,
                mapping.PayloadHash,
                previousPayloadHash,
                null);
        }

        return CreateItem(
            product,
            mapping.RetailerId,
            MetaCatalogSyncPlanOperations.Update,
            mapping.PayloadHash,
            previousPayloadHash,
            null);
    }

    private static bool HasSuccessfulPayloadHash(MetaProductSyncState? state)
    {
        return state != null
            && !string.IsNullOrWhiteSpace(state.LastPayloadHash);
    }

    private static MetaCatalogSyncPlanItem CreateItem(
        Product product,
        string retailerId,
        string operation,
        string? payloadHash,
        string? previousPayloadHash,
        string? reason)
    {
        return new MetaCatalogSyncPlanItem(
            product.Id,
            retailerId,
            product.Name,
            operation,
            payloadHash,
            previousPayloadHash,
            reason);
    }
}

public static class MetaCatalogSyncPlanOperations
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Unchanged = "UNCHANGED";
    public const string Delete = "DELETE";
    public const string AlreadyDeleted = "ALREADY_DELETED";
    public const string Skipped = "SKIPPED";
}

public record MetaCatalogSyncPlanResponse(
    bool DryRun,
    int Limit,
    MetaCatalogSyncPlanSummary Summary,
    IReadOnlyList<MetaCatalogSyncPlanItem> Items);

public record MetaCatalogSyncPlanSummary(
    int Scanned,
    int Create,
    int Update,
    int Unchanged,
    int Delete,
    int AlreadyDeleted,
    int Skipped)
{
    public static MetaCatalogSyncPlanSummary FromItems(IReadOnlyCollection<MetaCatalogSyncPlanItem> items)
    {
        return new MetaCatalogSyncPlanSummary(
            items.Count,
            Count(items, MetaCatalogSyncPlanOperations.Create),
            Count(items, MetaCatalogSyncPlanOperations.Update),
            Count(items, MetaCatalogSyncPlanOperations.Unchanged),
            Count(items, MetaCatalogSyncPlanOperations.Delete),
            Count(items, MetaCatalogSyncPlanOperations.AlreadyDeleted),
            Count(items, MetaCatalogSyncPlanOperations.Skipped));
    }

    private static int Count(IEnumerable<MetaCatalogSyncPlanItem> items, string operation)
    {
        return items.Count(i => string.Equals(i.Operation, operation, StringComparison.Ordinal));
    }
}

public record MetaCatalogSyncPlanItem(
    Guid ProductId,
    string RetailerId,
    string Name,
    string Operation,
    string? PayloadHash,
    string? PreviousPayloadHash,
    string? Reason);
