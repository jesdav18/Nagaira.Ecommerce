using Nagaira.Ecommerce.Application.DTOs;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IReportService
{
    Task<SalesReportDto> GenerateSalesReportAsync(DateTime startDate, DateTime endDate);
    Task<InventoryReportDto> GenerateInventoryReportAsync();
    Task<byte[]> ExportSalesReportToExcelAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> ExportInventoryReportToExcelAsync();
}

