namespace Nagaira.Ecommerce.Application.MetaCatalog;

public static class MetaCatalogAdminStatuses
{
    public const string NotSynced = "NOT_SYNCED";
    public const string Synced = "SYNCED";
    public const string UpdateAvailable = "UPDATE_AVAILABLE";
    public const string Processing = "PROCESSING";
    public const string Error = "ERROR";
    public const string NotEligible = "NOT_ELIGIBLE";
}

public record MetaCatalogAdminSummary(int Total, int Synced, int NotSynced, int UpdateAvailable, int Processing, int Errors, int NotEligible, bool AdminSyncEnabled);
public record MetaCatalogAdminProduct(Guid ProductId, string Name, string Sku, Guid? BrandId, string? BrandName, string? ImageUrl, bool IsEligible, string? EligibilityReason, string MetaStatus, string PlannedOperation, DateTime? LastSyncedAt, DateTime? LastAttemptAt, string? LastErrorMessage, bool PayloadChanged);
public record MetaCatalogAdminProductsResponse(int Page, int PageSize, int TotalCount, bool AdminSyncEnabled, IReadOnlyList<MetaCatalogAdminProduct> Items);
public record MetaCatalogSyncSelectedRequest(IReadOnlyList<Guid> ProductIds, bool Force = false);
public record MetaCatalogSyncOneRequest(bool Force = false);
