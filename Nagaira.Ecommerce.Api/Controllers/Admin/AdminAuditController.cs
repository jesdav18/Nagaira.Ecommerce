using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminAuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AdminAuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetAuditLogs(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var filter = new AuditLogFilterDto(
            userId,
            action,
            entityType,
            startDate,
            endDate,
            pageNumber,
            pageSize
        );

        var result = await _auditService.GetAuditLogsAsync(filter);
        return Ok(result);
    }

    [HttpGet("entity/{entityType}/{entityId:guid}")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogsByEntity(
        string entityType,
        Guid entityId)
    {
        var logs = await _auditService.GetAuditLogsByEntityAsync(entityType, entityId);
        return Ok(logs);
    }
}

