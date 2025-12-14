using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public AdminReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        if (start > end)
            return BadRequest("La fecha de inicio debe ser anterior a la fecha de fin");

        var report = await _reportService.GenerateSalesReportAsync(start, end);
        return Ok(report);
    }

    [HttpGet("sales/export")]
    public async Task<IActionResult> ExportSalesReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        if (start > end)
            return BadRequest("La fecha de inicio debe ser anterior a la fecha de fin");

        var fileContent = await _reportService.ExportSalesReportToExcelAsync(start, end);
        var fileName = $"reporte_ventas_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
        
        return File(fileContent, "text/csv", fileName);
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryReportDto>> GetInventoryReport()
    {
        var report = await _reportService.GenerateInventoryReportAsync();
        return Ok(report);
    }

    [HttpGet("inventory/export")]
    public async Task<IActionResult> ExportInventoryReport()
    {
        var fileContent = await _reportService.ExportInventoryReportToExcelAsync();
        var fileName = $"reporte_inventario_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        
        return File(fileContent, "text/csv", fileName);
    }
}

