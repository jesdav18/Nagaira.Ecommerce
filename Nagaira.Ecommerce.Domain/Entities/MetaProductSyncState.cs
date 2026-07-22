namespace Nagaira.Ecommerce.Domain.Entities;

public class MetaProductSyncState
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string RetailerId { get; set; } = string.Empty;
    public string Status { get; set; } = MetaProductSyncStatuses.Pending;
    public string? LastPayloadHash { get; set; }
    public string? PendingPayloadHash { get; set; }
    public string? LastAction { get; set; }
    public string? BatchHandle { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorSubcode { get; set; }
    public string? LastErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public Guid? LockId { get; set; }
    public DateTime? LockedUntilAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public static class MetaProductSyncStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Processing = "Processing";
    public const string Error = "Error";
    public const string Synced = "Synced";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";
}
