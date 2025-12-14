using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IAuditService
{
    Task LogActionAsync(Guid userId, string action, string entityType, Guid? entityId, string? oldValues = null, string? newValues = null, string? ipAddress = null, string? userAgent = null);
    Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filter);
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByEntityAsync(string entityType, Guid entityId);
}

