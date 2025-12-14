using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;
using Nagaira.Ecommerce.Domain.Interfaces;

namespace Nagaira.Ecommerce.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SalesReportDto> GenerateSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        var orders = await _unitOfWork.Admin.GetDeliveredOrdersByPeriodAsync(startDate, endDate);
        var totalRevenue = orders.Sum(o => o.Total);
        var totalOrders = orders.Count;
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        var salesByProductData = await _unitOfWork.Admin.GetSalesByProductAsync(startDate, endDate);
        var salesByProduct = salesByProductData.Select(s => new SalesByProductDto(
            s.ProductId,
            s.ProductName,
            s.QuantitySold,
            s.Revenue
        )).ToList();

        var salesByCategoryData = await _unitOfWork.Admin.GetSalesByCategoryAsync(startDate, endDate);
        var salesByCategory = salesByCategoryData.Select(s => new SalesByCategoryDto(
            s.CategoryId,
            s.CategoryName,
            s.QuantitySold,
            s.Revenue
        )).ToList();

        var salesByDayData = await _unitOfWork.Admin.GetSalesByDayAsync(startDate, endDate);
        var salesByDay = salesByDayData.Select(s => new SalesByDayDto(
            s.Date,
            s.Revenue,
            s.OrderCount
        )).ToList();

        return new SalesReportDto(
            startDate,
            endDate,
            totalRevenue,
            totalOrders,
            averageOrderValue,
            salesByProduct,
            salesByCategory,
            salesByDay
        );
    }

    public async Task<InventoryReportDto> GenerateInventoryReportAsync()
    {
        var inventoryItemsData = await _unitOfWork.Admin.GetInventoryItemsAsync();
        var totalProducts = inventoryItemsData.Count;
        var productsInStock = inventoryItemsData.Count(i => i.AvailableQuantity > 0);
        var productsOutOfStock = inventoryItemsData.Count(i => i.AvailableQuantity == 0);
        var productsLowStock = inventoryItemsData.Count(i => i.AvailableQuantity > 0 && i.AvailableQuantity <= 10);
        var totalInventoryValue = inventoryItemsData.Sum(i => i.TotalValue ?? 0);

        var inventoryItems = inventoryItemsData.Select(i => new InventoryItemDto(
            i.ProductId,
            i.ProductName,
            i.Sku,
            i.AvailableQuantity,
            i.AverageCost,
            i.TotalValue
        )).OrderByDescending(i => i.TotalValue).ToList();

        return new InventoryReportDto(
            DateTime.UtcNow,
            totalProducts,
            productsInStock,
            productsOutOfStock,
            productsLowStock,
            totalInventoryValue,
            inventoryItems
        );
    }

    public async Task<byte[]> ExportSalesReportToExcelAsync(DateTime startDate, DateTime endDate)
    {
        var report = await GenerateSalesReportAsync(startDate, endDate);
        
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        
        writer.WriteLine("Reporte de Ventas");
        writer.WriteLine($"Período: {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}");
        writer.WriteLine();
        writer.WriteLine($"Ingresos Totales: {report.TotalRevenue:C}");
        writer.WriteLine($"Total de Órdenes: {report.TotalOrders}");
        writer.WriteLine($"Valor Promedio de Orden: {report.AverageOrderValue:C}");
        writer.WriteLine();
        writer.WriteLine("Ventas por Producto:");
        writer.WriteLine("Producto,Cantidad Vendida,Ingresos");
        foreach (var item in report.SalesByProduct)
        {
            writer.WriteLine($"{item.ProductName},{item.QuantitySold},{item.Revenue:C}");
        }
        writer.WriteLine();
        writer.WriteLine("Ventas por Categoría:");
        writer.WriteLine("Categoría,Cantidad Vendida,Ingresos");
        foreach (var item in report.SalesByCategory)
        {
            writer.WriteLine($"{item.CategoryName},{item.QuantitySold},{item.Revenue:C}");
        }
        writer.WriteLine();
        writer.WriteLine("Ventas por Día:");
        writer.WriteLine("Fecha,Ingresos,Órdenes");
        foreach (var item in report.SalesByDay)
        {
            writer.WriteLine($"{item.Date:yyyy-MM-dd},{item.Revenue:C},{item.OrderCount}");
        }
        
        writer.Flush();
        return stream.ToArray();
    }

    public async Task<byte[]> ExportInventoryReportToExcelAsync()
    {
        var report = await GenerateInventoryReportAsync();
        
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        
        writer.WriteLine("Reporte de Inventario");
        writer.WriteLine($"Generado: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine();
        writer.WriteLine($"Total de Productos: {report.TotalProducts}");
        writer.WriteLine($"Productos en Stock: {report.ProductsInStock}");
        writer.WriteLine($"Productos Sin Stock: {report.ProductsOutOfStock}");
        writer.WriteLine($"Productos con Stock Bajo: {report.ProductsLowStock}");
        writer.WriteLine($"Valor Total del Inventario: {report.TotalInventoryValue:C}");
        writer.WriteLine();
        writer.WriteLine("Detalle de Inventario:");
        writer.WriteLine("SKU,Producto,Cantidad Disponible,Costo Promedio,Valor Total");
        foreach (var item in report.InventoryItems)
        {
            var cost = item.AverageCost.HasValue ? item.AverageCost.Value.ToString("C") : "N/A";
            writer.WriteLine($"{item.Sku},{item.ProductName},{item.AvailableQuantity},{cost},{item.TotalValue:C}");
        }
        
        writer.Flush();
        return stream.ToArray();
    }
}

