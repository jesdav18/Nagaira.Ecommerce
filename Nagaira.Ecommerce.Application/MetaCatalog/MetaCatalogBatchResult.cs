namespace Nagaira.Ecommerce.Application.MetaCatalog;

public record MetaCatalogBatchResult(IReadOnlyList<MetaCatalogItemResult> Items)
{
    public bool HasErrors => Items.Any(i => !i.Success);
}

public record MetaCatalogItemResult(
    string RetailerId,
    MetaCatalogSyncAction Action,
    bool Success,
    string? MetaItemId,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsTransient
);
