namespace Nagaira.Ecommerce.Application.DTOs;

public record AuditLogDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt
);

public record AuditLogFilterDto(
    Guid? UserId,
    string? Action,
    string? EntityType,
    DateTime? StartDate,
    DateTime? EndDate,
    int PageNumber = 1,
    int PageSize = 20
);

