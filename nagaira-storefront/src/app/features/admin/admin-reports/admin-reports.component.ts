import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-reports.component.html',
  styleUrls: ['./admin-reports.component.css']
})
export class AdminReportsComponent {
  private adminService = inject(AdminService);
  
  generating = signal(false);

  generateSalesReport(): void {
    this.generating.set(true);
    const endDate = new Date().toISOString().split('T')[0];
    const startDate = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    
    this.adminService.exportSalesReport(startDate, endDate).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `reporte_ventas_${startDate}_${endDate}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.generating.set(false);
      },
      error: (error: any) => {
        console.error('Error generating report:', error);
        alert('Error al generar el reporte');
        this.generating.set(false);
      }
    });
  }

  generateInventoryReport(): void {
    this.generating.set(true);
    this.adminService.exportInventoryReport().subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `reporte_inventario_${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.generating.set(false);
      },
      error: (error: any) => {
        console.error('Error generating report:', error);
        alert('Error al generar el reporte');
        this.generating.set(false);
      }
    });
  }
}

