using System.Text.Json;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogActionAsync(Guid userId, string action, string entityType, Guid? entityId, string? oldValues = null, string? newValues = null, string? ipAddress = null, string? userAgent = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs.AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filter)
    {
        var (totalCount, logs) = await _unitOfWork.AuditLogs.GetPagedAsync(
            filter.UserId,
            filter.Action,
            filter.EntityType,
            filter.StartDate,
            filter.EndDate,
            filter.PageNumber,
            filter.PageSize
        );

        var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

        var logDtos = logs.Select(a => new AuditLogDto(
            a.Id,
            a.UserId,
            $"{a.User.FirstName} {a.User.LastName}",
            a.Action,
            a.EntityType,
            a.EntityId,
            a.OldValues,
            a.NewValues,
            a.IpAddress,
            a.UserAgent,
            a.CreatedAt
        ));

        return new PagedResultDto<AuditLogDto>(
            logDtos,
            totalCount,
            filter.PageNumber,
            filter.PageSize,
            totalPages
        );
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByEntityAsync(string entityType, Guid entityId)
    {
        var logs = await _unitOfWork.AuditLogs.GetByEntityAsync(entityType, entityId);
        return logs.Select(a => new AuditLogDto(
            a.Id,
            a.UserId,
            $"{a.User.FirstName} {a.User.LastName}",
            a.Action,
            a.EntityType,
            a.EntityId,
            a.OldValues,
            a.NewValues,
            a.IpAddress,
            a.UserAgent,
            a.CreatedAt
        ));
    }
}

