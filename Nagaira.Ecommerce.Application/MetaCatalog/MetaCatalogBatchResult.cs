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
    bool IsTransient,
    string? Status = null,
    string? ErrorSubcode = null,
    IReadOnlyList<string>? Warnings = null,
    string? BatchHandle = null,
    string? ResponseContentType = null,
    int? ResponseBodyLength = null,
    IReadOnlyList<string>? ResponseTopLevelProperties = null,
    string? DiagnosticResponseBody = null
);
