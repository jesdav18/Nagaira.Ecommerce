using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId);
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null);
    Task<(int TotalCount, List<AuditLog> Logs)> GetPagedAsync(Guid? userId, string? action, string? entityType, DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize);
}

