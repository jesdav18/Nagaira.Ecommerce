import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';

@Component({
  selector: 'app-admin-inventory',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, AppCurrencyPipe],
  templateUrl: './admin-inventory.component.html',
  styleUrls: ['./admin-inventory.component.css']
})
export class AdminInventoryComponent implements OnInit {
  private adminService = inject(AdminService);
  
  report = signal<any>(null);
  loading = signal(true);
  alertFilter = signal<'all' | 'low' | 'out'>('all');

  ngOnInit(): void {
    this.loadInventoryReport();
  }

  loadInventoryReport(): void {
    this.loading.set(true);
    this.adminService.getInventoryReport().subscribe({
      next: (report: any) => {
        this.report.set(report);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading inventory report:', error);
        this.loading.set(false);
      }
    });
  }

  getAlertProducts(): any[] {
    const report = this.report();
    if (!report || !report.inventoryItems) return [];
    
    if (this.alertFilter() === 'low') {
      return report.inventoryItems.filter((item: any) => item.availableQuantity > 0 && item.availableQuantity <= 10);
    } else if (this.alertFilter() === 'out') {
      return report.inventoryItems.filter((item: any) => item.availableQuantity === 0);
    }
    return report.inventoryItems.filter((item: any) => item.availableQuantity <= 10);
  }

  getTopValuableProducts(): any[] {
    const report = this.report();
    if (!report || !report.inventoryItems) return [];
    return report.inventoryItems
      .filter((item: any) => item.totalValue && item.totalValue > 0)
      .sort((a: any, b: any) => (b.totalValue || 0) - (a.totalValue || 0))
      .slice(0, 10);
  }
}

